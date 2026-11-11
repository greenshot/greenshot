# Divoom

A Divoom pixel display, e.g. the Pixoo 64, shows a small picture of 64 × 64 pixels. Greenshot captures an area of exactly that size
and sends it to the display in your network: an icon, a part of a chart, a face from a video call.
The plugin is `Greenshot.Plugin.Divoom`, a **beta** feature of the full Greenshot (not in Greenshot Light).

- [For users](#for-users)
- [For developers](#for-developers)

---

## For users

### Turn it on

The plugin is only there for beta testers: in the settings, *Expert*, check *Enable to enable beta-test features*
(`BetaTester=True` in `[Core]` of greenshot.ini) and restart Greenshot. The installer has it as *Divoom plug-in (beta)*,
in the full and the custom installation.

### Set up the device

The device has to be in the same network as the computer (Wi-Fi), as for the Divoom app. In the settings, *Plugins*, *Divoom*:

- **Address of the device:** its IP address, e.g. `192.168.1.20`. The Divoom app shows it in the settings of the device.
  **Find devices** asks the server of Divoom (`app.divoom-gz.com`) which of your devices are in this network and lists them,
  picking one takes its address. Only finding the devices goes to the Internet, the pictures go straight to the device.
- **Size of the screen:** 64 × 64 pixels for the Pixoo 64; 16 × 16 and 32 × 32 for devices with a smaller screen and the same
  HTTP interface.
- **Send test picture** shows colors with a white frame on the device: when it appears, Greenshot can reach it.

Devices only reachable with Bluetooth (Timebox Evo, Pixoo Max) can't be used yet.

### Capture for Divoom

*Capture for Divoom* in the tray menu starts the [fixed size capture](capture-user-guide.md#fixed-size) with only the size of the
screen of the device. The preview of the selection (`P`) shows the area enlarged, pixel for pixel, as the device will show it.
A click or `Enter` sends it; `Esc` cancels. These captures don't change the last fixed size and aren't added to *Recently captured*.

The *Divoom* destination is also in the list of destinations, e.g. in the editor: a capture of another size is made as large as
fits on the screen of the device, in the middle, with black around it.

### When it doesn't arrive

The notification after the capture says that the device didn't take the picture and why, e.g. the device can't be reached: is it switched on, in the same
network, and is the address still right? The router can give the device another address; a fixed address in the router avoids that.
The log names the address of the device but never the picture.

## For developers

- **Protocol:** every command is a JSON object posted to `http://<device>/post`, the device answers `{"error_code": 0}`. A picture is
  `Draw/SendHttpGif` with one frame (`PicNum` 1, `PicWidth` the size, `PicData` the RGB bytes row by row as base64) and a `PicID`
  higher than the last one (`Draw/GetHttpGifId`); after a few pictures the id starts again (`Draw/ResetHttpGifId`), as the other
  libraries for the device do. Finding devices is `GET https://app.divoom-gz.com/Device/ReturnSameLANDevice`.
  Taken from [Divoom.Api](https://github.com/PanoramicData/Divoom.Api) (MIT) and the documentation of Divoom; Divoom.Api itself
  targets .NET 10 only, so the few commands are in `Device/DivoomClient.cs` instead of a package reference.
- **Network:** the device is reached without the proxy (a proxy can't reach the local network) and without redirects, through an own
  `HttpService`; finding devices uses `HttpService.Current`. Nothing that is sent is logged.
- **Capture:** the built-in recipe `recipe_divoom` (Source Region without the cursor, InteractiveSelection with
  `"FixedCaptureArea": "${config.Divoom.PanelSize}x${config.Divoom.PanelSize}"`, Destinations `Divoom`, Notification) is registered by
  the plugin as `IBuiltInRecipeProvider`. `FixedCaptureArea` starts the fixed size tool with only that entry, see
  [capture-tools.md](capture-tools.md).
- **Settings:** `[Divoom]` in greenshot.ini: `DeviceAddress`, `DeviceName`, `PanelSize`.
