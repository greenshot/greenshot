# IPC in Greenshot

Inter-Process Communication (IPC) in Greenshot is implemented using **Named Pipes**. This allows different Greenshot processes to communicate, for example, to pass command-line arguments from a new instance to an already running instance.

## Mechanism

The IPC mechanism works as follows:

1.  **Listener**: When a Greenshot instance starts, it creates an `IpcListener`. This listener opens a `NamedPipeServerStream` on a unique pipe name, which is generated using the prefix `Greenshot_Pipe_ID` and the current process ID (e.g., `Greenshot_Pipe_ID1234`). It then waits for incoming connections asynchronously.

2.  **Sender**: When another Greenshot instance needs to send a command (e.g., because it was launched with a filename as an argument while another instance was already running), it uses the `IpcSender`.

3.  **Broadcast**: The `IpcSender` finds all running processes with the name `Greenshot`. For each of these processes, it attempts to connect to its named pipe. Under normal circumstances, only a single, other than the current, Greenshot process exists, as the application itself implements a check to prevent multiple concurrent instances.

4.  **Serialization**: The commands to be sent are encapsulated in an `AppCommands` object, which is then placed inside an `IpcMessage` DTO. This `IpcMessage` is serialized into a byte array using the `MessagePack` library.

5.  **Transmission**: The serialized byte array is written to the named pipe using a `NamedPipeClientStream`.

6.  **Receiving and Deserialization**: The `IpcListener` of the receiving instance reads the byte array from the pipe, deserializes it back into an `IpcMessage` using `MessagePack`, and extracts the `AppCommands`.

7.  **Command Handling**: The received `AppCommands` are then processed by the main application logic (specifically, in `MainForm.HandleAppCommands`).

## Message Structure

The core message object is `IpcMessage`, which has the following structure:

```csharp
[MessagePackObject]
public class IpcMessage
{
    [Key(0)]
    public Guid MessageId { get; set; }

    [Key(1)]
    public int SenderPid { get; set; }

    [Key(2)]
    public List<KeyValuePair<CommandEnum, string>> Commands { get; set; }
}
```

-   `MessageId`: A unique identifier for the message.
-   `SenderPid`: The process ID of the sender.
-   `Commands`: A list of commands to be executed by the receiver.

## Available Commands

The possible commands are defined in the `CommandEnum` enumeration:

-   `OpenFile`: Instructs the running instance to open a specified file. The file path is passed as the string value in the command's `KeyValuePair`.
-   `Exit`: Requests the running instance to shut down gracefully.
-   `FirstLaunch`: A special command used internally on the first run after installation to trigger initial setup actions, like showing a ballon tip.
-   `ReloadConfig`: Instructs the running instance to reload its configuration from the `.ini` file.

## Command-line Handling

The `AppCommands` system is not only used for communication between different Greenshot processes but is also central to handling command-line arguments. When a new Greenshot instance is launched, it parses the command-line arguments and converts some of them into a series of `AppCommands`.
In the End it is processed in the same function `MainForm.HandleAppCommands`.
