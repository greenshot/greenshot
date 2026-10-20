# Greenshot 1.4 (vNext) documentation

These documents describe Greenshot 1.4, which is in development on the `main` branch. The features are not in Greenshot 1.3 or
work differently there, and they can still change before the release. The documentation of the current release is in the
[parent folder](../README.md).

**For users**

| Document | What it is about |
|---|---|
| [capture-user-guide.md](capture-user-guide.md) | Taking a screenshot: modes, helpers, keys and settings |
| [capture-recipes.md](capture-recipes.md) | Recipes: automating what happens with a capture |
| [custom-uploader.md](custom-uploader.md) | Uploading to a service of your choice with an uploader file (.sxcu) |
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
| [capture-tools.md](capture-tools.md) | How the capture modes and helpers are built, and how a plugin adds its own |
| [notifications.md](notifications.md) | Notifications: toasts and the tray icon |
| [http.md](http.md) | Talking to services on the Internet with `HttpService` |
| [greenshot-proxy-and-integration.md](greenshot-proxy-and-integration.md) | The native proxy, IPC, command line and the browser extension |
| [async-threading-roadmap.md](async-threading-roadmap.md) | The threading model and the async rules |
| [recipe-contracts-cli-query-plan.md](recipe-contracts-cli-query-plan.md) | Plan: recipe contracts, CLI queries and error handling |
| [recipe.schema.json](recipe.schema.json), [examples](examples) | The JSON schema of recipes and example recipes |
| [upstream](upstream/wpf-notifyicon-prompt.md) | Changes Greenshot would like in libraries it uses |
