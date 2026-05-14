import http from 'k6/http';
import ws from 'k6/ws';
import { check } from 'k6';
import { parseHTML } from 'k6/html';
import { Trend } from 'k6/metrics';

//to print a pretty html summary/report for our test results
// import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
// export function handleSummary(data) {
//   return {
//     "summary.html": htmlReport(data),
//   };
// }

const BASE_URL = 'http://192.168.178.12:5000'; //replace 192.168.178.12 with your own local ip
const WS_URL = 'ws://192.168.178.12:5000/hubs/notes';

const SIGNALR_HANDSHAKE_LAG = new Trend('signalr_handshake_lag');
const SIGNALR_NOTE_RTT_LAG = new Trend('signalr_note_rtt_lag'); //note's round-trip-time, latency: post note <-> receive note

const KILLSWITCH_DELAY = '10s';
const SIGNALR_ENDCHAR = String.fromCharCode(30); //signalr record separator

export const options = {
    scenarios: {
        // immediate_load_test: {
        //     executor: 'per-vu-iterations',
        //     vus: 2,
        //     iterations: 1,
        //     maxDuration: '2m',
        // },
        ramping_load_test: {
            executor: 'ramping-vus',
            startVUs: 0,
            stages: [
                { duration: '3m', target: 1000 },
                { duration: '30s', target: 1000 },
                { duration: '3m', target: 2000 },
                { duration: '30s', target: 2000 },
                { duration: '3m', target: 3000 },
                { duration: '30s', target: 3000 },
                { duration: '3m', target: 4000 },
                { duration: '30s', target: 4000 },
                { duration: '3m', target: 5000 },
                { duration: '30s', target: 5000 },
            ],
            gracefulStop: '0s', //abort test, since our test body runs infinitely
        },
    },
    thresholds: {
        http_req_failed: [{
            threshold: 'rate < 0.01',
            abortOnFail: true,
            delayAbortEval: KILLSWITCH_DELAY,
        }],
        ws_connecting: [{
            threshold: 'p(90) < 5000',
            abortOnFail: true,
            delayAbortEval: KILLSWITCH_DELAY
        }],
        signalr_handshake_lag: [{
            threshold: 'p(90) < 5000',
            abortOnFail: true,
            delayAbortEval: KILLSWITCH_DELAY,
        }],
        signalr_note_rtt_lag: [{
            threshold: 'p(90) < 3000',
            abortOnFail: true,
            delayAbortEval: KILLSWITCH_DELAY,
        }],
    },
};

///

export default function () {

    //login page, vu logs in, redirects to notes page

    let loginPage = http.get(`${BASE_URL}`);
    let requestToken = parseHTML(loginPage.body).find('input[name="__RequestVerificationToken"]').val();

    const loginResponse = http.post(`${BASE_URL}/Home/Login`, {
        Email: 'superadmin@email.local',
        Password: 'superadmin123!',
        __RequestVerificationToken: requestToken,
    });

    check(loginResponse, {
        'redirected to notes': (p) => p.url.includes('/notes'),
        'redirected after login, status correct': (p) => p.status === 200 || p.status === 302,
    });

    //notes page, every vu posts notes to everyone else continuously + measuring signalr latency/load

    const jar = http.cookieJar();
    const cookies = jar.cookiesForURL(BASE_URL);
    let cookieHeader = Object.keys(cookies).map(n => `${n}=${cookies[n][0]}`).join('; ');

    const pendingRequests = {};
    let requestCounter = 0;
    const handshakeStartTime = Date.now();

    //websocket, connecting to signalr notes hub
    ws.connect(WS_URL, { headers: { 'Cookie': cookieHeader } }, function (socket) {

        //open socket + handshake
        socket.on('open', () => {
            const handshakeLag = Date.now() - handshakeStartTime;
            SIGNALR_HANDSHAKE_LAG.add(handshakeLag);
            //console.log(`VU ${__VU} connected in ${handshakeLag}ms`);

            socket.send(`{"protocol":"json","version":1}${SIGNALR_ENDCHAR}`);
        });

        socket.on('message', (data) => {

            //handle handshake response
            if (data.includes('{}')) {
                //console.log(`handshake ok for vu ${__VU}`);

                //initiating different user types: 0-80 = reading only, 81-95 = casual posters, 96-100 = power posters
                const userType = Math.floor(Math.random() * 100);
                if (userType > 80) {
                    const postInterval = (userType > 95) ? 10000 : 60000; // 10s vs 60s

                    //handle note posting with a time interval
                    socket.setInterval(() => {
                        requestCounter++;
                        const requestId = `vu${__VU}_req${requestCounter}`;
                        pendingRequests[requestId] = Date.now(); //saving post timestamp per requestid
                        const msg = {
                            type: 1,
                            target: "AddNote",
                            arguments: [requestId],
                            invocationId: requestId
                        };
                        socket.send(`${JSON.stringify(msg)}${SIGNALR_ENDCHAR}`); //adding note via signalr
                        //console.log(`VU ${__VU} sent requestId: ${requestId}`);
                    }, postInterval);
                }
            }

            //handle received notes
            const messages = data.split(SIGNALR_ENDCHAR);
            messages.forEach(rawMsg => {
                if (!rawMsg || rawMsg === '{}') return; //skip empty/handshake
                try {
                    const parsed = JSON.parse(rawMsg);
                    if (parsed.invocationId && pendingRequests[parsed.invocationId]) { //retrieving post timestamp from requestid
                        const postStartTime = pendingRequests[parsed.invocationId];
                        const postLag = Date.now() - postStartTime; //calculating latency betweeen sending and receiving post
                        SIGNALR_NOTE_RTT_LAG.add(postLag); //adding to k6-trend, so threshold kill switches test on overload
                        delete pendingRequests[parsed.invocationId];
                        //console.log(`Lag for ${parsed.invocationId}: ${lag}ms`);
                    }
                } catch (e) {
                    //sink to handle cases where non-json data might appear
                }
            });
        });
    });
}