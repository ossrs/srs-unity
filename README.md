# srs-unity

[![](https://badgen.net/discord/members/yZ4BnPmHAd)](https://discord.gg/yZ4BnPmHAd)

Video Streaming and WebRTC Samples for Unity, please read this [link]() for detail.

Unity supports WebRTC, see [com.unity.webrtc](https://docs.unity3d.com/Packages/com.unity.webrtc@2.4/manual/index.html) or [github](https://github.com/Unity-Technologies/com.unity.webrtc). However, the demos only work in P2P mode, not with remote SFU or SRS.

To work with SFU or WebRTC server, the best practice is to use [WHIP](https://datatracker.ietf.org/doc/draft-ietf-wish-whip/) for Unity to publish to SFU, such as SRS. Actually, you're also able to play stream by WHIP.

The most common use scenaio for publishing stream, is to covert video game as live streaming. However you can use OBS to capture the window and audio, but it enable you to capture from Unity inside, cool! It works like this:

```
(Scenario 1)
Unity Game ---WebRTC----> SRS --+-----RTMP--> YouTube/FFmpeg
                                +---WebRTC--> H5/Unity
```

And the playing stream can be used for streaming consuming, it's a new feature enabled by Unity WebRTC. It works like this:

```
                                        (Scenario 2)
OBS/FFmpeg ----RTMP----+--> SRS --WebRTC--> Unity Game
H5/Unity -----WebRTC-----+
```

It also allows multiple Unity Games to communicate by WebRTC. And there should be a set of new use scenarios for Unity+WebRTC+SRS, that you can finger out and please let us know

<a name="usage"></a>

## Usage: Publisher

To publish your WebCamera and Microphone using WebRTC. If you're stuck, please get help from [Discord](https://discord.gg/yZ4BnPmHAd).

**Step 1:** Download and setup [Unity Hub](https://unity.com/download).

1. Open `Unity Hub`.
1. Click `Installs > Install Editor`, please select one release to install.
1. Check modules for `Unity Editor`, make sure `Visual Studio` installed.

**Step 2:** Create a empty`3D Core` project.

1. Open `Unity Hub`.
1. Click `Projects > New Project`.
1. Select `3D Core` template.
1. Set the `Project Name` to `My SRS Demos`.
1. Click `Create project`, and an `Unity Editor` will be opened.

**Step 3:** Install dependency package [com.unity.webrtc](https://docs.unity3d.com/Packages/com.unity.webrtc@2.4/manual/install.html).

1. Click `Window > Package Manager`.
1. Click `+ > Add package from git URL`.
1. Input `com.unity.webrtc` then click `Add`.

**Step 4:** Install [srs-unity](https://github.com/ossrs/srs-unity) package.

1. Download package [Video.Streaming.and.WebRTC.Samples.unitypackage](https://github.com/ossrs/srs-unity/releases/latest/download/Video.Streaming.and.WebRTC.Samples.unitypackage).
1. Click `Asserts > Import Package > Custom Package`, select the file `Video.Streaming.and.WebRTC.Samples.unitypackage`, then click `Import`.
1. From `Project` panel, open `Asserts > ossrs.io > Video Streaming and WebRTC Samples`, where you got all samples there.

**Step 5:** Start [SRS](https://ossrs.io/lts/en-us/docs/v5/doc/getting-started) WebRTC media server:

```bash
CANDIDATE="192.168.1.10"
docker run --rm -it -p 1935:1935 -p 1985:1985 -p 8080:8080 \
    --env CANDIDATE=$CANDIDATE -p 8000:8000/udp \
    ossrs/srs:5 ./objs/srs -c conf/docker.conf
```

> Note: Make sure your SRS is `v4.0.264+` or `v5.0.62+`. Please read this [guide](https://ossrs.io/lts/en-us/docs/v5/doc/getting-started) to setup SRS.

> Note: Please remember to replace the `CANDIDATE` to your server IP, please read [link](https://ossrs.io/lts/en-us/docs/v5/doc/webrtc#config-candidate) for details.

> Note: For online service, you might need authentication and other features, please read [How to Setup a Video Streaming Service by 1-Click](https://ossrs.io/lts/en-us/blog/SRS-Cloud-Tutorial) or [How to Setup a Video Streaming Service with aaPanel](https://ossrs.io/lts/en-us/blog/BT-aaPanel) to build a service by SRS.

**Step 6:** Work with `Publisher` sample.

1. From `Project` panel, open `Asserts > ossrs.io > Video Streaming and WebRTC Samples > Publisher`, then open the `Scene`.
1. Click `Edit > Play` to play Unity scene, which publish WebRTC stream to SRS.
1. Play the WebRTC stream by [H5](http://localhost:8080/players/rtc_player.html?autostart=true).

> Note: Note that the `Publisher` scene require the WebCamera and Microphone permission, you can try other sample if no device.

The stream flows like this:

```
Unity ---WebRTC---> SRS --WebRTC--> H5/Chrome
(WebCamera and Microphone)
```

> Note: You could use other WebRTC media server and client to replace SRS and Chrome.

https://user-images.githubusercontent.com/2777660/189331198-e045aeec-ad75-447b-8143-3b6d779e8dde.mp4

> Note: The latency is extremely low, which allows you to communicate with another Unity App. For example, you can use this mode to build a metting application by Unity.

## Usage: Streamer

To publish your game camera and voice using WebRTC. If you're stuck, please get help from [Discord](https://discord.gg/yZ4BnPmHAd).

**Step 1:** Download and setup [Unity Hub](https://unity.com/download).

1. Open `Unity Hub`.
1. Click `Installs > Install Editor`, please select one release to install.
1. Check modules for `Unity Editor`, make sure `Visual Studio` installed.

**Step 2:** Create a empty`3D Core` project.

1. Open `Unity Hub`.
1. Click `Projects > New Project`.
1. Select `3D Core` template.
1. Set the `Project Name` to `My SRS Demos`.
1. Click `Create project`, and an `Unity Editor` will be opened.

**Step 3:** Install dependency package [com.unity.webrtc](https://docs.unity3d.com/Packages/com.unity.webrtc@2.4/manual/install.html).

1. Click `Window > Package Manager`.
1. Click `+ > Add package from git URL`.
1. Input `com.unity.webrtc` then click `Add`.

**Step 4:** Install [srs-unity](https://github.com/ossrs/srs-unity) package.

1. Download package [Video.Streaming.and.WebRTC.Samples.unitypackage](https://github.com/ossrs/srs-unity/releases/latest/download/Video.Streaming.and.WebRTC.Samples.unitypackage).
1. Click `Asserts > Import Package > Custom Package`, select the file `Video.Streaming.and.WebRTC.Samples.unitypackage`, then click `Import`.
1. From `Project` panel, open `Asserts > ossrs.io > Video Streaming and WebRTC Samples`, where you got all samples there.

**Step 5:** Start [SRS](https://ossrs.io/lts/en-us/docs/v5/doc/getting-started) WebRTC media server:

```bash
CANDIDATE="192.168.1.10"
docker run --rm -it -p 1935:1935 -p 1985:1985 -p 8080:8080 \
    --env CANDIDATE=$CANDIDATE -p 8000:8000/udp \
    ossrs/srs:5 ./objs/srs -c conf/docker.conf
```

> Note: Make sure your SRS is `v4.0.264+` or `v5.0.62+`. Please read this [guide](https://ossrs.io/lts/en-us/docs/v5/doc/getting-started) to setup SRS.

> Note: Please remember to replace the `CANDIDATE` to your server IP, please read [link](https://ossrs.io/lts/en-us/docs/v5/doc/webrtc#config-candidate) for details.

> Note: For online service, you might need authentication and other features, please read [How to Setup a Video Streaming Service by 1-Click](https://ossrs.io/lts/en-us/blog/SRS-Cloud-Tutorial) or [How to Setup a Video Streaming Service with aaPanel](https://ossrs.io/lts/en-us/blog/BT-aaPanel) to build a service by SRS.

**Step 6:** Work with `Streamer` sample.

1. From `Project` panel, open `Asserts > ossrs.io > Video Streaming and WebRTC Samples > Streamer`, then open the `Scene`.
1. Click `Edit > Play` to play Unity scene, which publish WebRTC stream to SRS.
1. Play the WebRTC stream by [H5](http://localhost:8080/players/rtc_player.html?autostart=true).

> Note: Note that the `Streamer` scene grab an extra camera in game and voice of `Main Camera`, however there is no voice so you can add some audio clip to play.

The stream flows like this:

```
Unity ---WebRTC---> SRS --WebRTC--> H5/Chrome
(Game camera and voice)
```

> Note: You could use other WebRTC media server and client to replace SRS and Chrome.

https://user-images.githubusercontent.com/2777660/189329527-f7569003-f047-449e-8f48-ec59ddee60e4.mp4

> Note: A rotating cube is in the video stream, which is also the demostration of WebRTC.

https://user-images.githubusercontent.com/2777660/189353556-047025b2-2019-4c5a-a813-7a4c888840eb.mp4

> Note: Please note that we also capture the audio of game, so when we mute the H5 player there is no audio.

## Usage: Streamer

To play stream using WebRTC. If you're stuck, please get help from [Discord](https://discord.gg/yZ4BnPmHAd).

> Note: The stream might be published by another WebRTC client, or live stream like OBS or FFmpeg

**Step 1:** Download and setup [Unity Hub](https://unity.com/download).

1. Open `Unity Hub`.
1. Click `Installs > Install Editor`, please select one release to install.
1. Check modules for `Unity Editor`, make sure `Visual Studio` installed.

**Step 2:** Create a empty`3D Core` project.

1. Open `Unity Hub`.
1. Click `Projects > New Project`.
1. Select `3D Core` template.
1. Set the `Project Name` to `My SRS Demos`.
1. Click `Create project`, and an `Unity Editor` will be opened.

**Step 3:** Install dependency package [com.unity.webrtc](https://docs.unity3d.com/Packages/com.unity.webrtc@2.4/manual/install.html).

1. Click `Window > Package Manager`.
1. Click `+ > Add package from git URL`.
1. Input `com.unity.webrtc` then click `Add`.

**Step 4:** Install [srs-unity](https://github.com/ossrs/srs-unity) package.

1. Download package [Video.Streaming.and.WebRTC.Samples.unitypackage](https://github.com/ossrs/srs-unity/releases/latest/download/Video.Streaming.and.WebRTC.Samples.unitypackage).
1. Click `Asserts > Import Package > Custom Package`, select the file `Video.Streaming.and.WebRTC.Samples.unitypackage`, then click `Import`.
1. From `Project` panel, open `Asserts > ossrs.io > Video Streaming and WebRTC Samples`, where you got all samples there.

**Step 5:** Start [SRS](https://ossrs.io/lts/en-us/docs/v5/doc/getting-started) WebRTC media server:

```bash
CANDIDATE="192.168.1.10"
docker run --rm -it -p 1935:1935 -p 1985:1985 -p 8080:8080 \
    --env CANDIDATE=$CANDIDATE -p 8000:8000/udp \
    ossrs/srs:5 ./objs/srs -c conf/docker.conf
```

> Note: Make sure your SRS is `v4.0.264+` or `v5.0.62+`. Please read this [guide](https://ossrs.io/lts/en-us/docs/v5/doc/getting-started) to setup SRS.

> Note: Please remember to replace the `CANDIDATE` to your server IP, please read [link](https://ossrs.io/lts/en-us/docs/v5/doc/webrtc#config-candidate) for details.

> Note: For online service, you might need authentication and other features, please read [How to Setup a Video Streaming Service by 1-Click](https://ossrs.io/lts/en-us/blog/SRS-Cloud-Tutorial) or [How to Setup a Video Streaming Service with aaPanel](https://ossrs.io/lts/en-us/blog/BT-aaPanel) to build a service by SRS.

**Step 6:** Work with `Player` sample.

1. Publish the WebRTC stream by [H5](http://localhost:8080/players/rtc_publisher.html?autostart=true).
1. From `Project` panel, open `Asserts > ossrs.io > Video Streaming and WebRTC Samples > Player`, then open the `Scene`.
1. Click `Edit > Play` to play Unity scene, which publish WebRTC stream to SRS.

> Note: You're also able to publish a live stream and play it in Unity using WebRTC, see [WebRTC for Live Streaming](https://ossrs.io/lts/en-us/docs/v5/doc/getting-started#webrtc-for-live-streaming).

The stream flows like this:

```
H5/Chrome ---WebRTC---> SRS --WebRTC--> Unity
```

> Note: You could use other WebRTC media server and client to replace SRS and Chrome.

https://user-images.githubusercontent.com/2777660/189331098-bd7163cf-d6b9-480c-a204-ad8b721491b6.mp4

> Note: By converting live streaming to WebRTC, you're able to play normal stream from OBS or exists stream to Unity. For example, you can view a live sport or music by Unity.

