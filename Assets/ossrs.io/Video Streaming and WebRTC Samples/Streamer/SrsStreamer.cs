//
// Copyright (c) 2022 Winlin
//
// SPDX-License-Identifier: MIT
//
using System.Collections;
using UnityEngine;
using Unity.WebRTC;

public class SrsStreamer : MonoBehaviour
{
    // The WHIP stream url, to push WebRTC stream to SRS or other media servers.
    public string url = "http://localhost:1985/rtc/v1/whip/?app=live&stream=livestream";
    // The game camera as source of video stream. Generally you should create a
    // dedicated camera, not the main camera.
    //
    // @remark We will capture all audio of game, if bind this script to the
    // only one listener in scene, for example, bind to the `Main Camera`, so
    // that we could capture audio data in `OnAudioFilterRead`.
    public Camera sourceCamera;

    private WebCamTexture webCamTexture;
    private MediaStream videoStream;
    private AudioStreamTrack audioStreamTrack;
    private RTCPeerConnection pc;

    // To make the work flow better to understand, not required. You can
    // directly use the `OnAudioFilterRead` function to feed audio stream track.
    private delegate void DelegateOnAudioFilterRead(float[] data, int channels);
    private DelegateOnAudioFilterRead handleOnAudioFilterRead;

    private void Awake()
    {
        WebRTC.Initialize();
        Debug.Log("WebRTC: Initialize ok");
    }

    private void OnDestroy()
    {
        videoStream?.Dispose();
        videoStream = null;

        handleOnAudioFilterRead = null;

        audioStreamTrack?.Dispose();
        audioStreamTrack = null;

        pc?.Close();
        pc?.Dispose();
        pc = null;

        webCamTexture?.Stop();
        webCamTexture = null;

        WebRTC.Dispose();
        Debug.Log("WebRTC: Dispose ok");
    }

    void Start()
    {
        Debug.Log($"WebRTC: Start to stream {url}");

        // Start WebRTC update.
        StartCoroutine(WebRTC.Update());

        // Create object only after WebRTC initialized.
        pc = new RTCPeerConnection();

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
            Debug.Log($"WebRTC: OnTrack {e.Track.Kind} id={e.Track.Id}");
        };

        // Setup PeerConnection to send stream only.
        StartCoroutine(SetupPeerConnection());
        IEnumerator SetupPeerConnection()
        {
            RTCRtpTransceiverInit init = new RTCRtpTransceiverInit();
            init.direction = RTCRtpTransceiverDirection.SendOnly;
            pc.AddTransceiver(TrackKind.Audio, init);
            pc.AddTransceiver(TrackKind.Video, init);

            yield return StartCoroutine(GrabCamera());
        }

        // Grab the game camera.
        IEnumerator GrabCamera()
        {
            videoStream = sourceCamera.CaptureStream(480, 320);
            Debug.Log($"WebRTC: Grab camera stream={videoStream.Id}, size={sourceCamera.targetTexture.width}x{sourceCamera.targetTexture.height}");

            foreach (var track in videoStream.GetTracks())
            {
                pc.AddTrack(track);
                Debug.Log($"WebRTC: Add {track.Kind} track, id={track.Id}");
            }

            yield return StartCoroutine(GrabAudio());
        }

        // Grab the game audio, from the only one listener, the main camera,
        // that script should be attached to. And we will get audio data from
        // the function this.OnAudioFilterRead.
        IEnumerator GrabAudio()
        {
            // Use empty contructor to use listener, see https://docs.unity3d.com/Packages/com.unity.webrtc@2.4/manual/audiostreaming.html
            audioStreamTrack = new AudioStreamTrack();
            pc.AddTrack(audioStreamTrack);
            Debug.Log($"WebRTC: Add audio track, id={audioStreamTrack.Id}");

            // When got audio data from listener, the gameObject this script
            // attached to, generally the MainCamera object, we feed audio data to
            // audio stream track.
            handleOnAudioFilterRead = (float[] data, int channels) =>
            {
                if (audioStreamTrack != null)
                {
                    const int sampleRate = 48000;
                    audioStreamTrack.SetData(data, channels, sampleRate);
                }
            };

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

    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (handleOnAudioFilterRead != null)
        {
            handleOnAudioFilterRead(data, channels);
        }
    }

    void Update()
    {
    }
}
