# Async & Threading Roadmap

Plan to replace the sync/async workarounds in Greenshot (core, pipeline, destinations, plugins) with one clear
threading model and async APIs all the way through. **No backwards compatibility for plugins**: plugin and
destination interfaces are redesigned, and all in-repo destinations and plugins are refactored in the same
branch. Everything lands as **one PR** (see section 7.0).

> Written 2026-09-28 against `main` @ `2e6dd8ce`, revised 2026-09-29. Another branch is still to be merged, so locate
> code by **type / member name** and re-run the inventory command in 1.1 before starting.
> Companion document: [capture-and-imaging-roadmap.md](capture-and-imaging-roadmap.md). The public interface
> changes here ship together with that roadmap's capture-model and document/view steps (section 6).

**Reading guide:** sections 2–3 are the rules everyone codes against, and section 3 is the one-page cheat sheet
for PR review. Sections 4–5 are the new types. Section 7 is the work plan.

---

## 1. Findings

### 1.1 Inventory (excluding tests)

| Pattern | Count | Notes |
|---|---:|---|
| `Task.Run(` | 35 | Mix of legitimate background work and sync-over-async wrappers |
| `new Thread(` + `SetApartmentState(STA)` | 16 / 23 | Ad-hoc STA threads for clipboard, WPF windows, MAPI, export |
| `.Invoke(` / `BeginInvoke` / `Dispatcher.Invoke` | 199 / 37 / 28 | Includes delegate invokes; many are UI marshalling |
| `SynchronizationContext` | 33 | Mostly `uiContext.Send(...)` = blocking marshal to UI |
| `GetAwaiter().GetResult()` | 15 | Sync over async |
| `.Result` / `.Wait()` | 3 + 3 real | Sync over async, `ProcessingTask.Wait()` |
| `Thread.Sleep` | 12 | Including on the UI thread |
| `Application.DoEvents` | 9 | Re-entrancy hazards |
| `WaitOne(` | 7 | Events instead of `TaskCompletionSource` |
| `async void` | 4 | 3 are UI event handlers, 1 WinRT `DataRequested` |

Re-run:
```
grep -rnE "Application\.DoEvents|GetAwaiter\(\)\.GetResult\(\)|\.Result\b|\.Wait\(|async void|Thread\.Sleep|new Thread\(|WaitOne\(|Task\.Factory\.StartNew|ApartmentState\.STA|uiContext\.Send|InvokeOnSta|Task\.Run\(|InvokeRequired|new TaskCompletionSource|new Progress<" --include=*.cs src | grep -v /obj/ | grep -v Greenshot.Tests
```
(`InvokeRequired`, `new TaskCompletionSource` and `new Progress<` were added. Each one hides a thread assumption,
see 1.5 and section 3.)

### 1.2 Root causes

1. **No defined thread for the pipeline.** `CaptureHelper.*` and `TriggerManager` start flows with
   `_ = CapturePipeline.Instance.ExecuteAsync(...)` from the UI thread (fire-and-forget). Most sources and many steps
   are synchronous (`Task.FromResult` / `Task.CompletedTask`: all `*CaptureSource`, `EffectCaptureStep`,
   `DrawableStep`, `ProcessorExecutionStep`, `ZxingStep`, `BoxStep`, `DropboxStep`, `OfficeStep`, ...).
   `ConfigureAwait(false)` only changes anything at an await that has **not already completed**. So the flow stays
   on the UI thread until the first real await, and which thread runs a step depends on what ran before it.
   Capture, effects and drawables can run on the UI thread.
2. **Synchronous, UI-coupled `IDestination`.** `ExportCapture(bool, ISurface, ICaptureDetails)` is sync, and
   `IDestination` also carries WinForms types (`ToolStripMenuItem`, `ContextMenuStrip`, `Keys`, `Image`).
   `DestinationDispatcher.InvokeOnSta` therefore runs **every** non-file destination on the UI thread via
   `uiContext.Send`. That includes the network uploads of Imgur/Box/Dropbox/Jira/Confluence. The waiting UI is then
   kept alive with `PleaseWaitForm` (own STA thread + `DoEvents`).
3. **The `Surface` is a WinForms `Control`** (see the imaging roadmap), so export has to touch it on the UI thread.
   Plugin steps then call it from `Task.Run` anyway (e.g. `ImgurStep` → `UploadToImgur(surface, ...)`). That is
   cross-thread access to a control and to non-thread-safe GDI+ objects.
4. **Background work tracked as a mutable `Task` on data.** `CaptureDetails.ProcessingTask` (OCR) is awaited with
   `.Wait()` in `DestinationExportStep`, `InteractiveSelectionStep` and `Win10OcrDestination` (UI thread → freeze).
   Results are merged with crop-offset bookkeeping.
5. **Sync network stack.** `NetworkHelper` (HttpWebRequest), `OAuthSession`, `OAuth2Helper`, `ImgurUtils`,
   `BoxUtils`. The OAuth code receivers use `WaitOne`, and `Confluence.cs` wraps every async Dapplo call in
   `Task.Run(...).GetAwaiter().GetResult()`.
6. **Two UI frameworks, many UI threads.** WinForms owns the main loop (`Application.Run(new MainForm())`). WPF
   windows (`BugReportWindow`, `SelfServiceWindow`, `RecipeApprovalWindow`, RecipeEditor) are shown on newly created
   STA threads when not already on one. `SimpleServiceProvider` hands out the UI `SynchronizationContext` and
   `TaskScheduler` as globals.
7. **STA requirements solved per call.** Clipboard (`ClipboardHelper` spawns an STA thread and retries with
   `Thread.Sleep(100)`), MAPI mail (`MailHelper`), `ExternalCommandDestination`, Toast notifications.

### 1.3 Work that currently runs on (or blocks) the UI thread but shouldn't

| Where | What |
|---|---|
| `DestinationDispatcher.InvokeOnSta` → `IDestination.ExportCapture` | All regular destinations incl. uploads, Office interop, e-mail |
| `DestinationDispatcher` (`.greenshot` format) | Serialisation via `uiContext.Send(SaveSurface...)` |
| Pipeline sources/steps started from hotkey | Screen capture, effects, drawables until the first real await |
| `Surface.ApplyBitmapEffect` | Effect on UI thread, `BackgroundForm` + `DoEvents` |
| `Win10OcrDestination` | `ProcessingTask.Wait()` + `Task.Run(...).Result` |
| `JiraDestination.DisplayIcon` | `GetIssueTypeBitmapAsync(...).Result` in a property getter (deadlock risk) |
| `ClipboardHelper` | Retry loop with `Thread.Sleep(100)` |
| `WindowDetails.Restore` | `DoEvents` + `Sleep(20)` loop up to 2 s |
| `ActiveWindowCaptureSource` | `Thread.Sleep(100)` ×2 |
| `PleaseWaitForm.ShowAndWait` (Box, Dropbox, Imgur, Jira, Confluence) | Blocks the caller, second STA thread for the dialog |
| `CaptureForm` ctor | `DoEvents` to close a previous instance |
| `MainForm` shutdown | `DoEvents` before `Application.Exit` |

### 1.4 Other sync-over-async / blocking spots
- `WindowsGraphicsCaptureInterop.CaptureWindowToBitmap` / `CaptureMonitorToBitmap`: `Task.Run(...).GetAwaiter().GetResult()`
  + `ManualResetEvent.WaitOne(1000)` for `FrameArrived`.
- `WindowsAppHelper`: `Task.Run(() => GetAppxLogoAsync(...)).GetAwaiter().GetResult()`.
- `WindowsGraphicsCaptureVideoSession.Dispose`: `StopAsync().GetAwaiter().GetResult()`.
- `InteractiveSelectionStep` / `Win10OcrProcessor`: `Task.Run(async () => await DoOcrAsync(...)).Result`.
- `CaptureForm`: `Task.Factory.StartNew(async ...)` (returns `Task<Task>`, easy to misuse).
- `DagExecutionEngine`: `Task.Run` for parallel branches and a fire-and-forget `_ = Task.Run` for bypassed nodes
  (exceptions only logged, not awaited, not cancelled on shutdown).
- `ConfluenceTreePicker`, `ConfluenceUpload`: `new Thread` for loading data.

### 1.5 Hidden thread-affinity hazards (not visible in the grep, will bite once the pipeline moves to the pool)

Moving the pipeline off the UI thread (Phase 1) turns these from "works by accident" into crashes or races. Each
one needs an audit item in Phase 1.

| Hazard | Why it breaks | Fix |
|---|---|---|
| **Events raised by pipeline/services with UI subscribers** (tray icon, recent-captures menu, editor refresh, notification balloons) | Handler now runs on a pool thread → cross-thread control access | List every `event` on non-UI types. Subscribers marshal via `IUiDispatcher`, or the type raises through a UI-aware publisher. Never "fix" it with `InvokeRequired` in the handler. |
| **`TaskCompletionSource` completed on the UI thread** (form close, dialog result) | Without `RunContinuationsAsynchronously`, the awaiting pipeline continues **inline on the UI thread**, and `ConfigureAwait(false)` doesn't help | Always `new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously)` (enforced, section 3) |
| **`new Progress<T>(...)` created on a pool thread** | `Progress<T>` captures the *current* `SynchronizationContext`; created on the pool it reports on the pool | Create `Progress<T>` in UI code only, or report through `IUserInteraction.RunWithProgressAsync` |
| **GDI+ objects (`Bitmap`, `Graphics`, `Font`, `Brush`) shared across threads** | GDI+ is not thread-safe per object → "Object is currently in use elsewhere" / `InvalidOperationException` under load | Single-owner rule: a bitmap has exactly one owner at a time; hand-over transfers ownership (section 3) |
| **DPI awareness per thread** | If the UI thread uses `SetThreadDpiAwarenessContext` differently from the process default, `GetWindowRect`, `GetCursorPos` and monitor APIs return *different, virtualized* coordinates on pool threads | Verify that the manifest declares PerMonitorV2 process-wide. Grep for `SetThreadDpiAwarenessContext`. Capture sources assert the expected awareness in debug builds. |
| **Shared mutable config / singletons** (`CoreConfiguration` / IniConfig sections, `CapturePipeline.Instance`, static caches, `WindowDetails` caches) | Concurrent flows (hotkey spam) read/write concurrently | Flows get an immutable config snapshot at start. Writes go through the config service (single writer, lock). Static caches become `ConcurrentDictionary` or get removed. |
| **Foreground window / cursor at trigger time** | Once capture runs later on the pool, "active window" may already have changed | Snapshot foreground HWND + cursor position synchronously in the trigger handler and pass them in `CaptureFlowContext` |
| **`async void` UI handlers** | Exceptions crash the process (WinForms) or go unobserved | One `AsyncCommand` / `FireAndLog` helper (Phase 5, but new code uses it from Phase 0) |

---

## 2. Target threading model

1. **Exactly one UI thread** (WinForms now, Avalonia `Dispatcher.UIThread` later). All windows (WinForms, WPF,
   Avalonia) live on it. No other thread ever creates UI.
2. **The pipeline and all services run on the thread pool.** The *only* place that hops from UI to pool is the
   flow runner entry point. Steps never call `Task.Run` to "get off the UI thread".
3. **UI access from background code is always async**: `await ui.InvokeAsync(...)`. Never `Send`, `Invoke`,
   `Dispatcher.Invoke` or `InvokeRequired` checks in non-UI code.
4. **Async all the way**: every I/O-bound API returns `Task`/`ValueTask` and takes a `CancellationToken`. No
   `.Result`, `.Wait()`, `GetAwaiter().GetResult()`, `Thread.Sleep`, `DoEvents` or `WaitOne`. These are replaced by
   `await`, `Task.Delay` and `TaskCompletionSource`.
5. **CPU-bound work** (encode, effects, OCR, quantize) runs inline on the pool thread the pipeline is already on.
   `Task.Run` is allowed only at explicit boundaries (flow runner, UI event → background command) and for **true
   parallelism**. Note that `Task.WhenAll` over methods that complete synchronously runs them *sequentially*. If you
   want parallel CPU work, `Task.Run` each branch and document it.
6. **STA / COM** (clipboard OLE, Office interop, MAPI, WinRT share UI) goes through **one** of:
   - the UI thread via `IUiDispatcher` (short operations; clipboard *must* stay here, because the owning window has
     to outlive the data for delayed rendering), or
   - an `IStaWorker` (dedicated STA thread with message pump) for slow COM calls. There is **one per COM server**
     (Office, MAPI), so a hung Outlook cannot block a Word export or the UI.
7. **Immutable data crosses threads, mutable data has one owner.** Background code gets a rendered snapshot /
   document (imaging roadmap), never the UI control.
8. **Every started flow is tracked**: no fire-and-forget. A runner owns running flows, observes exceptions,
   applies a concurrency policy and cancels/awaits flows on shutdown.
9. `ConfigureAwait(false)` in core/library projects (enforced), not in UI projects.

### 2.1 Thread ownership table

| Component | Runs on | May block? | Talks to UI via |
|---|---|---|---|
| Hotkeys, tray, menus, editor, all windows | UI | Never | n/a |
| Flow runner entry (`Start`) | UI → hands off to pool | Never | n/a |
| Capture sources, steps, analyzers, DAG engine | Pool | Never (async I/O, inline CPU) | `IUiDispatcher`, `IUserInteraction` |
| Destinations | Pool | Never | `IUserInteraction` only |
| Clipboard | UI (short, via dispatcher) | Never | n/a |
| Office interop, MAPI | `IStaWorker` (per COM server) | Yes, that's what it's for | Never directly |
| Plugins `StartAsync` / `StopAsync` | Pool | Never | DI services |

```
 UI thread                         thread pool                         StaWorker (per COM server)
 ─────────                         ───────────                         ─────────
 hotkey ──► FlowRunner.Start ───►  pipeline (sources, steps, analyzers)
  (snapshot fg window, cursor)       │  await ui.InvokeAsync(selector) ◄─┐
 CaptureForm / dialogs ◄─────────────┘                                   │
            └── TCS (RunContinuationsAsynchronously) ──►│              │
                                     │  await destination.ExportAsync    │
                                     │     ├─ HttpClient / file I/O      │
                                     │     ├─ await ui.PickSaveFileAsync │
                                     │     └─ await sta.RunAsync(Office) ───►  COM interop
```

---

## 3. Coding rules (PR checklist)

Short enough to paste into the PR template. Each rule is backed by an analyzer where possible (Phase 0).

| # | Rule | Enforced by |
|---|---|---|
| R1 | No blocking waits: `.Result`, `.Wait()`, `GetAwaiter().GetResult()`, `WaitOne`, `Thread.Sleep`, `DoEvents` | VSTHRD002, BannedApi |
| R2 | No `async void` except through `AsyncCommand` / `FireAndLog` | VSTHRD100 |
| R3 | Every `Task` is awaited, returned, or handed to the flow runner | VSTHRD110, CA2012 |
| R4 | `TaskCompletionSource` always with `TaskCreationOptions.RunContinuationsAsynchronously` | BannedApi on the parameterless ctor (wrap in `Tcs.Create<T>()`) |
| R5 | Non-UI projects never reference `Control.Invoke`, `InvokeRequired`, `Dispatcher.Invoke`, `SynchronizationContext.Send`, `WindowsFormsSynchronizationContext` | BannedApi (per-project `BannedSymbols.txt`) |
| R6 | Every async method that does I/O or waits takes a `CancellationToken` and passes it on | CA2016 |
| R7 | **Cancellation convention:** cancellation (token fired) → throw `OperationCanceledException`. User declined in a dialog → `ExportStatus.Declined` result. Never swallow `OperationCanceledException` except at the flow runner. | Review |
| R8 | **Bitmap ownership:** a method that *returns* an image transfers ownership (caller disposes). A method that *receives* one borrows it (must not dispose or keep it). Cached images are exposed as leases (`IImageLease : IDisposable`). | Review, tests with a disposal-tracking image |
| R9 | `Progress<T>` only constructed in UI code | BannedApi in non-UI projects |
| R10 | `Task.Run` only in: flow runner, `AsyncCommand`, documented parallel branches (comment `// PARALLEL:` + reason) | BannedApi baseline + review |
| R11 | `ConfigureAwait(false)` in library projects | CA2007 (library projects only) |
| R12 | Async-only APIs: no `Foo()` + `FooAsync()` pairs, no sync wrappers "for convenience" | Review |

---

## 4. Core abstractions

### 4.1 UI dispatcher
```csharp
public interface IUiDispatcher
{
    bool CheckAccess();
    void VerifyAccess();                                   // throws if not on UI thread (debug + critical paths)
    Task InvokeAsync(Action action, CancellationToken ct = default);
    Task<T> InvokeAsync<T>(Func<T> func, CancellationToken ct = default);
    Task<T> InvokeAsync<T>(Func<Task<T>> func, CancellationToken ct = default); // for dialogs that complete later
}
```
Semantics (must be identical across implementations, and tested):
- If already on the UI thread, **still post** (never run inline). This avoids re-entrancy and makes behaviour
  independent of the caller's thread.
- `ct` cancels *before* the delegate starts. Once it is running, the delegate owns cancellation (it gets the token
  if it needs it).
- The returned task completes on the pool (TCS with `RunContinuationsAsynchronously`), never inline on the UI thread.
- Exceptions from the delegate propagate to the awaiter. Nothing is swallowed.

Implementations:
- `WpfUiDispatcher`: the one Greenshot uses, `DispatcherSynchronizationContext.Post` captured at startup (on the thread
  that later runs the WPF `Application.Run`, the message loop of Greenshot). The WinForms editor forms live on this
  thread too, `WindowsFormsHost.EnableWindowsFormsInterop` gives them their keyboard handling.
- `WinFormsUiDispatcher`: `WindowsFormsSynchronizationContext.Post` captured at startup (on the thread that later
  runs `Application.Run`). This avoids `Control.BeginInvoke`'s "handle not yet created" failure mode during
  startup and shutdown. After `ShutdownAsync` begins, new invokes fail fast with `ObjectDisposedException`
  instead of hanging.
  On .NET 9+ WinForms, check whether `Control.InvokeAsync` and the (experimental) `Form.ShowDialogAsync` can
  replace the hand-rolled parts.
- `AvaloniaUiDispatcher`: `Dispatcher.UIThread.InvokeAsync` (later).
- `InlineUiDispatcher` for tests, plus `StrictTestUiDispatcher` which runs a dedicated single thread and fails any
  test that touches UI objects from elsewhere.

### 4.2 STA worker
```csharp
public interface IStaWorker : IAsyncDisposable
{
    string Name { get; }                                   // "Office", "MAPI" (for logs and hang diagnostics)
    Task RunAsync(Action action, CancellationToken ct = default);
    Task<T> RunAsync<T>(Func<T> func, CancellationToken ct = default);
}
public interface IStaWorkerFactory { IStaWorker Get(string name); }  // one lazily created worker per name
```
- A dedicated STA thread with a real message loop (`Application.Run` of a hidden form, or a
  `GetMessage`/`DispatchMessage` pump). Needed for Outlook/Word callbacks and MAPI dialogs.
- Registers a COM **`IMessageFilter`** (`CoRegisterMessageFilter`) that retries `RPC_E_CALL_REJECTED` /
  `SERVERCALL_RETRYLATER`, the classic "Office is busy" failure. This replaces ad-hoc retry loops in the Office
  plugin.
- Watchdog: a call running longer than N seconds is logged with the worker name. Cancellation can't abort a COM
  call, but the flow can stop *awaiting* it (`WaitAsync(ct)`), and the worker is marked unhealthy and recreated
  for the next call.
- Replaces every `new Thread(...) { ApartmentState.STA }` in `MailHelper`, `ExternalCommandDestination`,
  `DestinationDispatcher.InvokeOnSta` and `ToastNotificationService`. Clipboard is **not** on this list (UI thread, 2.6).

### 4.3 User interaction (replaces dialogs inside destinations)
```csharp
public interface IUserInteraction
{
    bool IsInteractive { get; }
    Task<string?> PickSaveFileAsync(SaveFileRequest request, CancellationToken ct);
    Task<OutputSettings?> PromptOutputSettingsAsync(OutputSettings current, CancellationToken ct);
    Task<IDestination?> PickDestinationAsync(IReadOnlyList<IDestination> choices, CancellationToken ct);
    Task<TResult?> ShowDialogAsync<TResult>(IDialogViewModel<TResult> viewModel, CancellationToken ct);

    // Replaces PleaseWaitForm / BackgroundForm: work runs on the pool, dialog is non-blocking, has Cancel.
    // Dialog only appears if work takes > ~300 ms (no flicker for fast uploads).
    Task<T> RunWithProgressAsync<T>(string title,
        Func<IProgress<ProgressInfo>, CancellationToken, Task<T>> work, CancellationToken ct);

    Task NotifyAsync(Notification notification);
}
```
- `InteractiveUserInteraction` (UI) and `HeadlessUserInteraction` (unattended recipes / CLI / tests: returns
  configured defaults or throws `InteractionRequiredException`). The recipe decides which one a flow gets.
- `ShowDialogAsync` is typed (`IDialogViewModel<TResult>`) instead of `object`, so a mismatched result type is a
  compile error, not a runtime cast.
- Modal dialogs are serialized: at most one modal prompt at a time, app-wide (`SemaphoreSlim(1)` inside the
  interactive implementation). Parallel flows then queue their prompts instead of stacking modal loops.
- `INotificationService` folds into `NotifyAsync`.

### 4.4 Flow runner (single entry point)
```csharp
public interface ICaptureFlowRunner
{
    CaptureFlowHandle Start(CaptureRecipe recipe, FlowTriggerContext trigger,
                            Action<CaptureFlowContext>? configure = null);
    IReadOnlyCollection<CaptureFlowHandle> Running { get; }
    Task ShutdownAsync(CancellationToken ct);  // stop accepting, cancel all, await all (with timeout)
}
public sealed record FlowTriggerContext(ITrigger? Trigger, IntPtr ForegroundWindow, PixelPoint CursorPosition,
                                        DateTimeOffset TriggeredAt);   // snapshotted on the UI thread (see 1.5)
public sealed class CaptureFlowHandle
{
    public Guid Id { get; }
    public Task<CaptureFlowResult> Completion { get; }   // never faults: faults become CaptureFlowResult.Failed
    public void Cancel();
}
```
- `Start` does the one `Task.Run(() => pipeline.ExecuteAsync(...))`, registers the handle, and logs/notifies
  faults. It is the only place that turns an exception into a user notification (destinations just throw or return
  a result).
- **Concurrency policy per recipe** (`FlowConcurrency`): `Parallel` (default for unattended/upload flows),
  `Exclusive` (interactive region capture: a second hotkey press while `CaptureForm` is open is ignored or brings it
  to front, instead of today's `DoEvents` close-and-reopen), `ReplacePrevious`.
- Replaces all `_ = CapturePipeline.Instance.ExecuteAsync(...)` in `CaptureHelper`, `TriggerManager`,
  `DynamicDestinationWindow`, RecipeEditor and `DagExecutionEngine` (error-recipe execution).
- Hooks `TaskScheduler.UnobservedTaskException` and logs with flow id. Anything that shows up there is a bug
  (should be zero, see the "done" criteria).

---

## 5. Redesigned public interfaces

All types below use the capture model from the imaging roadmap (`CaptureMetadata`, `AnnotationDocument`). On net48,
before that exists, `IExportSource` wraps today's `ISurface` (section 6).

### 5.1 Destination
```csharp
public interface IDestination
{
    string Designation { get; }
    DestinationDescriptor Descriptor { get; }          // display name, icon key, priority, shortcut (neutral types)
    bool IsAvailableFor(CaptureMetadata metadata);     // must be cheap and non-blocking (called while building menus)
    ValueTask<IReadOnlyList<IDestination>> GetDynamicDestinationsAsync(CaptureMetadata metadata, CancellationToken ct);
    Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken ct);
}

public sealed record ExportRequest(
    IExportSource Source,
    CaptureMetadata Metadata,
    bool ManuallyInitiated,
    IUserInteraction Ui,
    IProgress<ProgressInfo>? Progress);

public interface IExportSource                        // one per flow, owned by the flow, disposed when the flow ends
{
    AnnotationDocument Document { get; }               // read-only snapshot
    Task<IImageLease> RenderAsync(OutputSettings settings, CancellationToken ct);      // cached per settings; lease, not ownership (R8)
    Task<EncodedImage> EncodeAsync(OutputSettings settings, CancellationToken ct);     // cached per settings
}

public sealed class EncodedImage
{
    public string MimeType { get; }
    public string FileExtension { get; }
    public ReadOnlyMemory<byte> Bytes { get; }
    public Stream OpenRead();                          // fresh, independent stream per call → safe for parallel destinations
}

public enum ExportStatus { Succeeded, Declined, Failed }
public sealed record ExportResult(ExportStatus Status, string? FilePath = null, Uri? Uri = null, string? Error = null)
{
    public static ExportResult Declined { get; } = new(ExportStatus.Declined);   // user said no; cancellation throws (R7)
    public static ExportResult Failed(string error) => new(ExportStatus.Failed, Error: error);
}
```
Changes vs. the first draft:
- `EncodeAsync` returns `EncodedImage` instead of `Stream`. A shared cached `Stream` can't be read by two
  destinations at once and has unclear disposal. Bytes + `OpenRead()` fix both.
- `RenderAsync` returns a lease, so a destination can't dispose or mutate the cached image.
- `ExportResult` has an explicit status. Previously "cancelled" and "failed without message" were the same value.

Removed: `ExportCapture`, `ExportInformation` (mutable), `IAcceptsPreRenderedImage`,
`CapturePayload.SharedRenderedBitmap` (replaced by `IExportSource` caching), `GetMenuItem(...)`,
`EditorShortcutKeys`, `DisplayIcon` as `System.Drawing.Image`, `IsActive`, `UseDynamicsOnly`, `IsLinkable`
(fold into descriptor), `IComparable`/`IDisposable` on destinations.
Menus are built in the UI layer by a `DestinationMenuBuilder` from descriptors. Icons that need network (Jira issue
types) are resolved by an `IIconProvider` asynchronously. The menu shows a placeholder and updates, and a getter never
blocks.

**Export order:** destinations of one flow run **sequentially** in recipe order by default, because a later one
may depend on an earlier one's result (e.g. "upload, then copy link to clipboard"). A recipe can mark a group as
parallel. The shared `IExportSource` cache makes that safe.

### 5.2 Capture sources, steps, analyzers
```csharp
public interface ICaptureSource { Task<Capture> AcquireAsync(CaptureFlowContext ctx, CancellationToken ct); }
public interface ICaptureStep   { Task ExecuteAsync(CaptureFlowContext ctx, CancellationToken ct); }  // unchanged shape

// Replaces IProcessor + CaptureDetails.ProcessingTask/Features
public interface ICaptureAnalyzer
{
    string Name { get; }
    Task AnalyzeAsync(CapturedImage image, AnalysisResults results, CancellationToken ct);
}
public sealed class AnalysisResults   // lives on the flow context
{
    public Task<IReadOnlyList<OcrLine>> Ocr { get; }        // started once (lazily on first access or eagerly per recipe), awaited by consumers
    public Task<IReadOnlyList<Barcode>> Barcodes { get; }
    // positions in screen coordinates → no CropOffset bookkeeping
}
```
`IOcrProvider` → `Task<IReadOnlyList<OcrLine>> RecognizeAsync(Image<Bgra32>, PixelRect screenArea, string? language, CancellationToken)`.
Analyzers get their own linked token. When the flow finishes, unfinished analysis is cancelled, not leaked.

### 5.3 Interactive selection
```csharp
public interface IInteractiveCaptureSelector
{
    // Called from the pool; implementation does await ui.InvokeAsync(...) and completes via TaskCompletionSource
    // (RunContinuationsAsynchronously) when the CaptureForm closes. No uiContext.Send, no DoEvents.
    Task<SelectionResult?> SelectAsync(CapturedImage fullscreen, IReadOnlyList<WindowInfo> windows,
                                       CaptureKind initialMode, CancellationToken ct);
}
```
Cancelling `ct` closes the form (via the dispatcher) and completes with `OperationCanceledException`. Esc by the
user returns `null` (= declined).

### 5.4 Plugins
```csharp
public interface IGreenshotPlugin : IAsyncDisposable
{
    string Name { get; }
    void ConfigureServices(IServiceCollection services);         // registration only, sync, no I/O
    Task StartAsync(IServiceProvider services, CancellationToken ct);
    Task StopAsync(CancellationToken ct);
}
public interface IConfigurablePlugin { object CreateSettingsViewModel(IServiceProvider services); }
```
- Host starts plugins **in parallel** with a per-plugin timeout, and the main window doesn't wait for them. A slow
  or failing plugin is logged and disabled, and never blocks startup.
- Removed: `RegisterConfiguration(IniConfig)`, `RegisterServices(IServiceLocator)`, `bool Start()`, `Shutdown()`,
  `Configure()`, `CreateConfigurationControl()` (WPF `UIElement`). `IGreenshotHost` goes away. Plugins receive
  services (`ICaptureFlowRunner`, `IUserInteraction`, `IUiDispatcher`, `IStaWorkerFactory`, `IHttpClientFactory`,
  config) through DI.

### 5.5 Services that become async
| Today | Target |
|---|---|
| `NetworkHelper` (HttpWebRequest) | Shared `HttpClient` (via factory), async upload with progress & cancellation. On net48 use one long-lived `HttpClient` per host (socket exhaustion), on .NET 10 `IHttpClientFactory`. |
| `OAuthSession`, `OAuth2Helper`, `LocalServerCodeReceiver`, `LocalJsonReceiver` | Async; `HttpListener.GetContextAsync` + TCS; `Task.Delay` instead of `Sleep`; timeout via token; token refresh serialized with `SemaphoreSlim` (parallel uploads must not refresh twice) |
| `ClipboardHelper.SetClipboardData / Get*` | `IClipboardService` async; runs on UI thread via dispatcher; retries (clipboard held by another process) with `Task.Delay` *between* dispatcher calls, so the UI keeps pumping |
| `ImageIO.Save*` | `Task SaveAsync(IExportSource, path, settings, ct)`; write to temp file + atomic rename (a cancelled save never leaves half a file) |
| `MailHelper` (MAPI) | `IStaWorker("MAPI")`, no `WaitOne(60s)` |
| `WindowsGraphicsCaptureInterop` | `Task<CapturedImage> CaptureAsync(...)`; `FrameArrived` → TCS with timeout via `CancellationTokenSource` |
| `WindowDetails.Restore` | `Task RestoreAsync(ct)` polling with `Task.Delay` or WinEvent hook + TCS |
| `WindowsAppHelper.GetAppxLogo` | Async all the way, callers await |
| Video `Dispose` → `StopAsync().GetResult()` | `IAsyncDisposable` |

**net48 package notes:** `IAsyncDisposable` / `ValueTask` need `Microsoft.Bcl.AsyncInterfaces`;
`Process.WaitForExitAsync` doesn't exist (use `Exited` + TCS); `Task.WaitAsync` doesn't exist (write a small
`WithCancellation` extension, delete it on .NET 10).

---

## 6. Relationship to the imaging roadmap

| Imaging roadmap step | Async work that belongs with it |
|---|---|
| Step 1 (WGC-only) | Async capture backend (`IScreenCaptureBackend` is already async), TCS for `FrameArrived` |
| Step 2 (capture model) | `AnalysisResults`, `ICaptureAnalyzer`, remove `ProcessingTask`, `ICaptureSource` returning `Capture` |
| Step 3 (document/view) | `IExportSource` over `AnnotationDocument`; destinations never touch the view |
| Step 4 (.NET 10) | Generic Host + `Microsoft.Extensions.DependencyInjection`, `IHostedService` for triggers / IPC / updates, `Process.WaitForExitAsync`, `IAsyncDisposable` natively |
| Step 6 (Avalonia) | `AvaloniaUiDispatcher`; all windows on one dispatcher |

**Bridge before the document exists:** `SurfaceExportSource` implements `IExportSource` by calling
`surface.GetImageForExport()` **once** via `await ui.InvokeAsync(...)`, taking ownership of the returned bitmap,
then encodes/caches on the pool. That alone removes the need to run destinations on the UI thread. It is the
single most valuable change in this plan, which is why it is in Phase 2 and not later.

---

## 7. Phased plan

### 7.0 Delivery: one branch, one PR
- All phases are done on a single branch and merged as **one PR** once everything works. There are no
  compatibility shims or adapters: old interfaces are deleted and every destination/plugin in the repo is ported
  directly.
- The phases are the **order of work inside the branch**, not release units. The build may be red between phases.
  The per-phase "Done when" items are checkpoints to verify before moving on, so problems are found in the phase
  that caused them and not at the end.
- **Scope of the PR:** Phases 0–3 and 5. Phase 4 needs imaging roadmap step 2 (capture model) and Phase 6 needs the
  .NET 10 move (step 4). Include them if those steps are already in, otherwise they follow in the PR for that step.
- Rebase on `main` early and often: the other branch still to be merged touches the same types, and one large PR
  is only reviewable if the diff isn't also carrying merge noise.

### 7.1 Order of work
```
Phase 0 ──► Phase 1 ──► Phase 2 ──► Phase 3 ──► Phase 5 ──► merge (one PR)
                                        (Phase 4: with imaging step 2, Phase 6: with .NET 10)
```

### Phase 0: Guardrails & measurement (small, do first)
Goal: make regressions impossible and freezes measurable *before* changing behaviour.
- [x] Add `Microsoft.VisualStudio.Threading.Analyzers` (VSTHRD002, VSTHRD100, VSTHRD101, VSTHRD103, VSTHRD110,
      VSTHRD200) as warnings.
- [x] Add `Microsoft.CodeAnalysis.BannedApiAnalyzers`. There are **two** `BannedSymbols.txt` files: a strict one for
      core/library/plugin projects (everything in R1, R4, R5, R9) and a lighter one for UI projects (R1 only;
      `Control.Invoke` is legitimate there). Start with a suppression baseline and burn it down per phase; it must
      be empty before the PR is opened.
- [x] CA2007 (`ConfigureAwait`) for library projects only; CA2016 (forward `CancellationToken`) everywhere.
- [x] **UI-stall watchdog** (debug + opt-in in release): a background timer posts to the UI thread every 100 ms
      and logs a warning with the currently running flow step when a post takes > 250 ms. This produces the
      baseline "freezes per capture" number and proves each phase helped.
- [x] Debug-only thread assertions: `ui.VerifyAccess()` in `Surface`/editor entry points, and
      `ThreadAssert.NotUi()` at the start of every pipeline step.
- [x] Introduce `IUiDispatcher`, `IStaWorker(Factory)`, `Tcs.Create<T>()`, `AsyncCommand` (implementations + tests
      for the semantics in 4.1).

Done when: analyzers run with a committed baseline, and the watchdog numbers for "region capture → file",
"→ clipboard" and "→ Imgur" are recorded.

> Status: analyzers are at error level with an empty baseline. The watchdog numbers still have to be recorded
> manually (they need a real desktop session) and attached to the PR.

### Phase 1: Pipeline threading (internal, no public API change)
Goal: every flow runs on the pool, is tracked and is cancellable.
- [x] **Audit 1.5 first** (events with UI subscribers, TCS creation, `Progress<T>`, DPI, shared config). This is
      required before the pipeline moves, or Phase 1 introduces crashes.
- [x] `ICaptureFlowRunner` with concurrency policy and `FlowTriggerContext` snapshot; replace all
      `_ = ExecuteAsync(...)` call sites.
- [x] Remove `Task.Run` inside steps (`InteractiveSelectionStep` window snapshot, `ImgurStep`, `ConfluenceStep`,
      `ExternalCommandStep`).
- [x] `DagExecutionEngine`: parallel branches keep `Task.Run` (true parallelism, R10) but are awaited via
      `Task.WhenAll`; bypassed-node runs are awaited or registered with the runner; cancellation is propagated.
- [x] `InteractiveCaptureSelector` → `ui.InvokeAsync` + TCS on form close; `Exclusive` policy replaces the
      `CaptureForm` `DoEvents` close-previous logic.
- [x] `ProcessingTask.Wait()` → `await` (interim, until `AnalysisResults`).
- [x] OCR call sites: `await ocrProvider.DoOcrAsync(...)` instead of `Task.Run(...).Result`.
- [x] `ActiveWindowCaptureSource` / `WindowDetails.Restore`: `Task.Delay`.

Done when: `ThreadAssert.NotUi()` never fires, hotkey spam (20×) produces no exceptions, and region capture →
file shows no watchdog stall except while `CaptureForm` is open.

### Phase 2: Destination contract + network (the user-visible win)
Goal: no upload, e-mail or Office export ever freezes the UI. The async network stack is part of this phase
because an async upload destination is impossible without it.
- [x] Delete the old `IDestination`, `ExportInformation`, `IAcceptsPreRenderedImage`; add the new `IDestination` /
      `ExportRequest` / `IExportSource` / `EncodedImage` / `ExportResult` / `IUserInteraction`.
- [x] `SurfaceExportSource` bridge (section 6).
- [x] `HttpClient`-based upload helper with progress & cancellation; async OAuth (serialized refresh).
- [x] Port **all** destinations. Suggested order, simplest first so the contract is validated early:
      File, FileWithDialog, Clipboard, Printer → **Imgur, Box, Dropbox** (simple uploads) → **Jira, Confluence**
      (remove all `Task.Run(...).GetResult()`, `new Thread`, and the `DisplayIcon .Result` via `IIconProvider`) →
      Email (`IStaWorker("MAPI")`), Office (`IStaWorker("Office")` + message filter) → Editor, Picker,
      Win10 OCR, Win10 Share.
- [x] Delete `PleaseWaitForm`.
- [x] `DestinationDispatcher`: remove `InvokeOnSta`, `uiContext.Send`, shared-bitmap handling, quality-dialog
      marshalling → `await ui.PromptOutputSettingsAsync`.

Done when: upload of a 4K screenshot with network throttled to 1 Mbit/s shows no watchdog stall, cancel works
mid-upload, and `NetworkHelper`'s sync paths are deleted.

### Phase 3: Plugins lifecycle, UI menus
- [x] `DestinationMenuBuilder` (tray menu, editor, picker) from descriptors.
- [x] New `IGreenshotPlugin` lifecycle (parallel start with timeout); remove `IGreenshotHost` and `IServiceLocator`
      usage in plugins.
- [x] ExternalCommand: `Process` + `Exited` event/TCS (net48) → `WaitForExitAsync` (.NET 10); no STA thread.
- [x] Zxing: `ICaptureAnalyzer` (or interim async step if Phase 4 isn't ready).
- [x] RecipeEditor: open on UI thread via dispatcher (remove STA thread creation).

Done when: no plugin references WinForms/WPF types through the plugin contract, and `IGreenshotHost` /
`IServiceLocator` no longer exist.

### Phase 4: Analysis (with imaging roadmap step 2)
- [ ] `ICaptureAnalyzer` + `AnalysisResults`; delete `IProcessor`, `ProcessingTask`, `StartedProcessors`,
      `CropOffset` handling.

### Phase 5: UI thread consolidation
- [x] All WPF windows on the main WinForms UI thread (`BugReportWindow`, `SelfServiceWindow`,
      `RecipeApprovalWindow`, RecipeEditor). No WPF `Application` object is needed: show them directly on the
      WinForms thread and call `ElementHost.EnableModelessKeyboardInterop(window)` for modeless windows so keyboard
      input works. Remove the per-window STA threads. This does **not** need to wait for Avalonia.
- [x] `Surface.ApplyBitmapEffect`: effect on the pool via `RunWithProgressAsync`, result applied on UI; delete
      `BackgroundForm`.
- [x] All async UI event handlers through `AsyncCommand` / `FireAndLog`.
- [x] Shutdown: `await runner.ShutdownAsync()`, `await plugin.StopAsync()`, dispose STA workers; no `DoEvents`.
      Use a bounded timeout (e.g. 5 s), then log what is still running and exit anyway.

### Phase 6: Hosting (with the .NET 10 move)
- [ ] Generic Host + `Microsoft.Extensions.DependencyInjection`; replace `SimpleServiceProvider` (≈150 uses).
- [ ] `IHostedService` for `TriggerManager`, `NamedPipeServer`, `UpdateService`, hotkeys.
- [ ] Remove `SynchronizationContext` / `TaskScheduler` registrations from the service locator.
- [ ] Delete net48 shims (`WithCancellation`, `Exited`-TCS helpers); evaluate .NET 9+ WinForms `InvokeAsync` /
      `ShowDialogAsync` for `WinFormsUiDispatcher`.

### Done when (overall)
- Banned-API baseline is empty; VSTHRD analyzers at error level.
- No `Task.Run` outside the flow runner, `AsyncCommand` and `// PARALLEL:`-documented branches.
- No UI code outside UI projects; destinations/plugins touch UI only via `IUiDispatcher` / `IUserInteraction`.
- Every flow is observable via `CaptureFlowHandle.Completion` and cancellable.
- Watchdog: zero UI stalls > 250 ms in the standard scenarios (region → file / clipboard / Imgur / Outlook).
- `TaskScheduler.UnobservedTaskException` fires zero times in the stress suite.

---

## 8. Testing & diagnostics
- Unit tests with `InlineUiDispatcher` and `HeadlessUserInteraction`, so destinations and steps are testable
  without UI.
- `StrictTestUiDispatcher` + a test `SynchronizationContext` that throws when used from the pool (catches
  accidental UI-thread assumptions).
- Deadlock regression: run pipeline tests with a single-threaded context installed (`AsyncContext`-style) to
  surface sync-over-async.
- Disposal-tracking test image (`IImageLease` implementation that records double-dispose / use-after-dispose) to
  enforce R8.
- `IStaWorker` tests: a call that hangs doesn't block other workers; `WaitAsync(ct)` returns; worker is recreated.
- Stress: 20 parallel flows (hotkey spam) + shutdown while uploading → all flows complete or cancel, no leaks
  (handle count and GDI object count stable before/after, checked via `GetGuiResources`).
- Manual scenario script per phase with watchdog output attached to the PR.

---

## 9. Risks & open decisions

| Risk / question | Mitigation / proposal |
|---|---|
| Moving the pipeline to the pool exposes latent cross-thread bugs (1.5) | Audit before Phase 1; debug assertions; Phase 1 checkpoint (hotkey spam, no `ThreadAssert` hits) before starting Phase 2 |
| One large PR is hard to review and bisect | Keep one commit per phase (or per destination in Phase 2) on the branch; merge without squashing so history stays bisectable; attach the watchdog before/after numbers to the PR |
| Adding threads to capture adds latency between hotkey and screenshot | Snapshot trigger context synchronously; measure hotkey → pixels in Phase 0 and Phase 1; budget +5 ms |
| Office/MAPI hangs (security prompts, busy server) | Per-server `IStaWorker`, message filter, watchdog, flow stops awaiting on cancel |
| Parallel flows race on config / OAuth tokens | Config snapshot per flow; serialized token refresh |
| WPF modeless windows on the WinForms thread have keyboard/focus quirks | `EnableModelessKeyboardInterop`; test each window; fallback is to keep that single window on its own thread until Avalonia |
| **Open:** concurrency default for hotkey flows while `CaptureForm` is open: ignore, or bring to front? | Proposal: bring to front |
| **Open:** should a failed destination abort the remaining destinations in a flow? | Proposal: no, continue and report all results; recipe can opt into "stop on first failure" |

---

## 10. Decision log
| Date | Decision |
|---|---|
| 2026-09-28 | One UI thread, pipeline on the pool, async all the way, no plugin backwards compatibility |
| 2026-09-29 | No compatibility adapters; all in-repo destinations and plugins ported directly; one branch, one PR |
| 2026-09-29 | Clipboard on UI thread; COM via one `IStaWorker` per server with message filter |
| 2026-09-29 | Cancellation throws; user decline is a result status (`ExportStatus.Declined`) |
| 2026-09-29 | `IExportSource` returns leases and `EncodedImage`, not shared bitmaps/streams |
| 2026-09-29 | Network stack moves with the destination phase |
| 2026-09-29 | One `BannedSymbols.txt` for all non-test projects instead of a strict and a UI list; UI code uses `IUiDispatcher` too, so `Control.Invoke` isn't needed there either. Justified exceptions are `#pragma warning disable RS0030` with the rule in the comment |
| 2026-09-29 | `FlowConcurrency` is per recipe; the exclusivity of the interactive selection lives in `IInteractiveCaptureSelector` (`IsSelecting`, `BringToFront`): a hotkey while `CaptureForm` is open brings it to the front (open question 9 closed) |
| 2026-09-29 | A failed destination doesn't stop the others: the dispatcher exports to all destinations and reports all failures (`DestinationExportException` with the first exception, all errors in `DestinationExportErrors`), open question 9 closed |
| 2026-09-29 | `SurfaceExportSource.UseSurfaceAsync` is the bridge to the surface (UI thread) until the `AnnotationDocument` exists: editor hand-over, `.greenshot` format, upload URL |
| 2026-09-29 | `IUserInteraction.ConfirmAsync` added; `ExportResult` got `ClearsModified`, `KeepsCapture`, `Target` and `Exception` |
| 2026-09-29 | Configuration writes from flows are marshalled to the UI thread (single writer, the change events have UI subscribers) |
| 2026-09-29 | Parallel destination groups are not implemented: destinations run sequentially in recipe order, sharing one export source |
| 2026-09-29 | Windows Graphics Capture became async already in Phase 1 (`FrameArrived` → TCS) |
| 2026-09-29 | Plugin contract without DI container (net48): `ConfigureServices(IPluginServices)` with deferred factories for what needs the loaded configuration, `StartAsync(IServiceProvider)`; settings views are registered by the plugin (`AddSettingsView<TViewModel>`), the contract has no WinForms/WPF types. `SimpleServiceProvider` stays (thread safe, `IServiceProvider`) until Phase 6 |
| 2026-09-29 | Editor/capture form extension points (`IEditorPlugin`, `IFeatureHotspotTransformer`) keep their WinForms types until the document/view split of the imaging roadmap |
| 2026-09-29 | Justified dedicated threads: `IStaWorker`, the WASAPI real-time capture loop, and the crash report of a terminating process (its UI thread may be the problem) |
| 2026-09-29 | Phases 0–3 and 5 done on one branch; VSTHRD, RS0030, CA2016, CA2012 and CA2007 (library folders) are errors |
| 2026-09-29 | Recording finalization on suspend is fire-and-log, not a bounded wait: the SystemEvents callback can run on the UI thread, blocking it would stall the message pump that the finalization may need |
| 2026-09-29 | Cancelling an export closes what it opened: the progress dialog, the destination picker menu and the pipeline prompt/flyout windows close when their token is cancelled, and closing a progress dialog cancels its work |
| 2026-09-29 | Upload steps (Imgur, Box, Dropbox) log a failed upload to the flow log and let the flow continue; destinations report it as `ExportResult.Failed` |
| 2026-09-29 | Async caches never keep a failed or cancelled task: the next request retries |
