# Greenshot 1.4 (vNext) documentation

These documents describe Greenshot 1.4, which is in development on the `main` branch. The features are not in Greenshot 1.3 or
work differently there, and they can still change before the release. The documentation of the current release is in the
[parent folder](../README.md).

**For users**

| Document | What it is about |
|---|---|
| [capture-user-guide.md](capture-user-guide.md) | Taking a screenshot: modes, helpers, keys and settings |
| [capture-recipes.md](capture-recipes.md) | Recipes: automating what happens with a capture |
| [viewer.md](viewer.md) | The viewer: a capture in a small window of its own, e.g. on top to compare it |
| [image-formats.md](image-formats.md) | Which image formats Greenshot opens and saves, and more formats with codecs installed in Windows |
| [external-commands.md](external-commands.md) | External commands: handing a capture to another program, sharing a command as a file |
| [custom-uploader.md](custom-uploader.md) | Uploading to a service of your choice with an uploader file (.sxcu) |
| [editor-panels.md](editor-panels.md) | The panel area of the editor: the Selection panel with size, position and the emoji picker |
| [smart-text.md](smart-text.md) | Smart text: pick web addresses, phone numbers and own patterns from the text of a capture |
| [stamps.md](stamps.md) | Stamps: small images to insert into the editor or give to other programs |
| [self-service.md](self-service.md) | The Self-Service window: checking that Greenshot is set up as expected |
| [welcome-window.md](welcome-window.md) | The welcome window at the first start |
| [autostart.md](autostart.md) | Starting Greenshot with Windows |
| [plugins-and-destinations.md](plugins-and-destinations.md) | Switching plugins and destinations on or off, and locking it for all users |
| [plugin-loading.md](plugin-loading.md) | How plugins are loaded, the signature check and the question about unsigned plugins |
| [mcp-server.md](mcp-server.md) | Letting AI tools use Greenshot through greenshot-mcp.exe |
| [TEXT_OBFUSCATION_FEATURE.md](TEXT_OBFUSCATION_FEATURE.md) | Obfuscating text found with OCR |

**For developers**

| Document | What it is about |
|---|---|
| [code-structure.md](code-structure.md) | Where code goes and how it is named |
| [accessibility.md](accessibility.md) | Rules for screen readers, keyboard, high contrast and text size, and how to check them |
| [ui-how-to.md](ui-how-to.md) | Building windows and settings pages: styles, texts, settings |
| [translation](translation/README.md) | Language packs and how to translate Greenshot |
| [hdr.md](hdr.md) | HDR captures: SDR and HDR, tone mapping, the HDR image, the edits of the editor and writing JPEG XR files |
| [editor-panels.md](editor-panels.md#for-developers) | Adding panels and sections to the panel area of the editor |
| [capture-tools.md](capture-tools.md) | How the capture modes and helpers are built, and how a plugin adds its own |
| [notifications.md](notifications.md) | Notifications: toasts and the tray icon |
| [http.md](http.md) | Talking to services on the Internet with `HttpService` |
| [destination-definitions.md](destination-definitions.md) | Destinations made of code plus definitions (data), like the custom uploader |
| [greenshot-proxy-and-integration.md](greenshot-proxy-and-integration.md) | The native proxy, IPC, command line and the browser extension |
| [async-threading-roadmap.md](async-threading-roadmap.md) | The threading model and the async rules |
| [recipe-contracts-cli-query-plan.md](recipe-contracts-cli-query-plan.md) | Plan: recipe contracts, CLI queries and error handling |
| [expression-engine.md](expression-engine.md) | Design: one syntax and one resolver for variables (`${...}`) in settings, recipes and plugins |
| [recipe.schema.json](recipe.schema.json), [examples](examples) | The JSON schema of recipes and example recipes; more recipes are in [greenshot/greenshot-recipes](https://github.com/greenshot/greenshot-recipes) |
| [upstream](upstream/wpf-notifyicon-prompt.md) | Changes Greenshot would like in libraries it uses |
