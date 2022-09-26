//
// Copyright (c) 2022 Winlin
//
// SPDX-License-Identifier: MIT
//
using System.Collections;
using UnityEngine;
using Unity.WebRTC;

// See https://docs.unity.cn/Packages/com.unity.webrtc@2.4/manual/tutorial.html
public class SrsPlayer : MonoBehaviour
{
    // The WHIP stream url, to pull WebRTC stream from SRS or other media
    // servers. Please note that SRS uses `/rtc/v1/whip-play/` as a WebRTC
    // player, or parameter `action=play` in query string.
    public string url = "http://localhost:1985/rtc/v1/whip-play/?app=live&stream=livestream";
    // The RAW image to render the received WebRTC video stream. Generally, it
    // should be in a Canvas object. Please note that we will scale the image
    // size according to the video stream resolution.
    public UnityEngine.UI.RawImage receiveImage;
    // The audio source to play the received WebRTC audio stream. Please create
    // a normal audio source.
    public AudioSource receiveAudio;

    private MediaStream receiveStream;
    private RTCPeerConnection pc;

    private void Awake()
    {
        WebRTC.Initialize();
        Debug.Log("WebRTC: Initialize ok");
    }

    private void OnDestroy()
    {
        pc?.Close();
        pc?.Dispose();
        pc = null;

        WebRTC.Dispose();
        Debug.Log("WebRTC: Dispose ok");
    }

    private void Start()
    {
        Debug.Log($"WebRTC: Start to play {url}");

        // Start WebRTC update.
        StartCoroutine(WebRTC.Update());

        // Create object only after WebRTC initialized.
        pc = new RTCPeerConnection();
        receiveStream = new MediaStream();

        // Setup player peer connection.
        pc.OnIceCandidate = candidate =>
        {
            Debug.Log($"WebRTC: OnIceCandidate {candidate.ToString()}");
        };
        pc.OnIceConnectionChange = state =>
        {
            Debug.Log($"WebRTC: OnIceConnectionChange {state.ToString()}");
        };
        pc.OnTrack = e =>
        {
            receiveStream.AddTrack(e.Track);
        };

        // Setup player media stream.
        receiveStream.OnAddTrack = e =>
        {
            Debug.Log($"WebRTC: OnAddTrack {e.ToString()}");
            if (e.Track is VideoStreamTrack videoTrack)
            {
                videoTrack.OnVideoReceived += tex =>
                {
                    Debug.Log($"WebRTC: OnVideoReceived {videoTrack.ToString()}, tex={tex.width}x{tex.height}");
                    receiveImage.texture = tex;

                    var width = tex.width < 1280 ? tex.width : 1280;
                    var height = tex.width > 0 ? width * tex.height / tex.width : 720;
                    receiveImage.rectTransform.sizeDelta = new Vector2(width, height);
                };
            }
            if (e.Track is AudioStreamTrack audioTrack)
            {
                Debug.Log($"WebRTC: OnAudioReceived {audioTrack.ToString()}");
                receiveAudio.SetTrack(audioTrack);
                receiveAudio.loop = true;
                receiveAudio.Play();
            }
        };

        // Setup PeerConnection to receive stream only.
        StartCoroutine(SetupPeerConnection());
        IEnumerator SetupPeerConnection()
        {
            RTCRtpTransceiverInit init = new RTCRtpTransceiverInit();
            init.direction = RTCRtpTransceiverDirection.RecvOnly;
            pc.AddTransceiver(TrackKind.Audio, init);
            pc.AddTransceiver(TrackKind.Video, init);

            yield return StartCoroutine(PeerNegotiationNeeded());
        }

        // Generate offer.
        IEnumerator PeerNegotiationNeeded()
        {
            var op = pc.CreateOffer();
            yield return op;

            Debug.Log($"WebRTC: CreateOffer done={op.IsDone}, hasError={op.IsError}, {op.Desc}");
            if (op.IsError) yield break;

            yield return StartCoroutine(OnCreateOfferSuccess(op.Desc));
        }

        // When offer is ready, set to local description.
        IEnumerator OnCreateOfferSuccess(RTCSessionDescription offer)
        {
            var op = pc.SetLocalDescription(ref offer);
            Debug.Log($"WebRTC: SetLocalDescription {offer.type} {offer.sdp}");
            yield return op;

            Debug.Log($"WebRTC: Offer done={op.IsDone}, hasError={op.IsError}");
            if (op.IsError) yield break;

            yield return StartCoroutine(ExchangeSDP(url, offer.sdp));
        }

        // Exchange SDP(offer) with server, got answer.
        IEnumerator ExchangeSDP(string url, string offer)
        {
            // Use Task to call async methods.
            var task = System.Threading.Tasks.Task<string>.Run(async () =>
            {
                System.Uri uri = new System.UriBuilder(url).Uri;
                Debug.Log($"WebRTC: Build uri {uri}");

                var content = new System.Net.Http.StringContent(offer);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/sdp");

                var client = new System.Net.Http.HttpClient();
                var res = await client.PostAsync(uri, content);
                res.EnsureSuccessStatusCode();

                string data = await res.Content.ReadAsStringAsync();
                Debug.Log($"WebRTC: Exchange SDP ok, answer is {data}");
                return data;
            });

            // Covert async to coroutine yield, wait for task to be completed.
            yield return new WaitUntil(() => task.IsCompleted);
            // Check async task exception, it won't throw it automatically.
            if (task.Exception != null)
            {
                Debug.Log($"WebRTC: Exchange SDP failed, url={url}, err is {task.Exception.ToString()}");
                yield break;
            }

            StartCoroutine(OnGotAnswerSuccess(task.Result));
        }

        // When got answer, set remote description.
        IEnumerator OnGotAnswerSuccess(string answer)
        {
            RTCSessionDescription desc = new RTCSessionDescription();
            desc.type = RTCSdpType.Answer;
            desc.sdp = answer;
            var op = pc.SetRemoteDescription(ref desc);
            yield return op;

            Debug.Log($"WebRTC: Answer done={op.IsDone}, hasError={op.IsError}");
            yield break;
        }
    }

    private void Update()
    {
    }
}
