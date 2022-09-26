//
// Copyright (c) 2022 Winlin
//
// SPDX-License-Identifier: MIT
//
using System.Collections;
using UnityEngine;
using Unity.WebRTC;

public class SrsPublisher : MonoBehaviour
{
    // The WHIP stream url, to push WebRTC stream to SRS or other media servers.
    public string url = "http://localhost:1985/rtc/v1/whip/?app=live&stream=livestream";
    // A RAW image to render the camera stream, a preview of camera. Please note
    // that we will randomly choose a camera.
    public UnityEngine.UI.RawImage sourceImage;
    // A audio source to capture the audio of microphone. Please note that we
    // will randomly choose a microphone.
    public AudioSource sourceAudio;

    private WebCamTexture webCamTexture;
    private VideoStreamTrack videoStreamTrack;
    private AudioStreamTrack audioStreamTrack;
    private RTCPeerConnection pc;

    private void Awake()
    {
        WebRTC.Initialize();
        Debug.Log("WebRTC: Initialize ok");
    }

    private void OnDestroy()
    {
        videoStreamTrack?.Dispose();
        videoStreamTrack = null;

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
        Debug.Log($"WebRTC: Start to publish {url}");

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

            yield return StartCoroutine(StartCamera());
        }

        // Start cemara device.
        IEnumerator StartCamera()
        {
            // Request user authorization.
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                Debug.LogFormat($"WebRTC: Authorization for using the camera is denied");
                yield break;
            }

            // Select camera device.
            WebCamDevice device = new WebCamDevice();
            foreach (var cam in WebCamTexture.devices)
            {
                if (device.name == null || cam.name.ToLower().Contains("built-in")) device = cam;
                Debug.Log($"WebRTC: Camera device {cam.name} kind={cam.kind}, front={cam.isFrontFacing}");
            }
            Debug.Log($"WebRTC: Camera {device.name} selected");

            webCamTexture = new WebCamTexture(device.name);
            webCamTexture.Play();

            yield return new WaitUntil(() => webCamTexture.didUpdateThisFrame);
            Debug.Log($"WebRTC: Camera {device.name} kind={device.kind} is open, texture={webCamTexture.width}x{webCamTexture.height}");

            videoStreamTrack = new VideoStreamTrack(webCamTexture);
            sourceImage.texture = webCamTexture;
            sourceImage.rectTransform.sizeDelta = new Vector2(webCamTexture.width, webCamTexture.height);
            pc.AddTrack(videoStreamTrack);
            Debug.Log($"WebRTC: Add video track {videoStreamTrack.Id}");

            yield return StartCoroutine(StartMicrophone());
        }

        // Start microphone device.
        IEnumerator StartMicrophone()
        {
            // Request user authorization.
            yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                Debug.LogFormat($"WebRTC: Authorization for using the microphone is denied");
                yield break;
            }

            string device = null;
            foreach (var mic in Microphone.devices)
            {
                if (device == null || mic.ToLower().Contains("microphone")) device = mic;
                Debug.Log($"WebRTC: Microphone device {mic}");
            }
            Debug.Log($"WebRTC: Microphone {device} selected");

            Microphone.GetDeviceCaps(device, out int minFreq, out int maxFreq);
            var clip = Microphone.Start(device, true, 1, 48000);

            // set the latency to “0” samples before the audio starts to play.
            while (!(Microphone.GetPosition(device) > 0)) { }

            sourceAudio.clip = clip;
            sourceAudio.loop = true;
            sourceAudio.Play();
            audioStreamTrack = new AudioStreamTrack(sourceAudio);
            pc.AddTrack(audioStreamTrack);
            Debug.Log($"WebRTC: Add audio track {audioStreamTrack.Id}");

            yield return StartCoroutine(PeerNegotiationNeeded()); ;
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

    void Update()
    {
    }
}
