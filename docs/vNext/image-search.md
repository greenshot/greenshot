# Search the web for a capture

**Beta**, not in Greenshot Light. The destination **Search the web for this image** (`ImageSearch`) sends the capture to a search
engine and opens the results in the default browser: where an image comes from, similar images, the text in it.

| Engine | What happens |
|---|---|
| Bing | Greenshot writes a small page with the capture to its temp folder (`%TEMP%\Greenshot\Files`) and opens it in the default browser, the page sends the capture to Bing at once. The results open in your own browser session. The page is removed after a minute, at the latest when Greenshot exits. |
| Yandex | Greenshot uploads the capture to Yandex and opens the result page. Yandex keeps the image under a public address: anyone with the link can see it. |

In the menus the entry opens a sub menu with the engines; chosen as destination after a capture (or in a recipe) it uses Bing. It only works when someone is at the computer: recipes and
other unattended flows skip it.

## Before the capture is sent

Greenshot shows the capture with the engine and asks before it sends it. **Don't ask again for …** remembers the engine in
`ImageSearchConfirmedEngines` in the `[Core]` section of `greenshot.ini`; remove the name there to be asked again.

## Not Greenshot's responsibility

The engines have no official way for a desktop program to search with an image, Greenshot does what their own web pages do.
Greenshot only hands the capture over and isn't responsible for the search or what the engine does with the image.
When a change at an engine stops the search from working, that engine is removed from Greenshot.

Google Lens and TinEye aren't offered: Google shows its cookie consent page first (in the EU) and loses the posted image,
TinEye answers with a browser check.

An administrator can block the destination like any other: `BlockedDestinations=ImageSearch` (see [policies](policies.md)).
