# XUniDaq Integration Core
### High-Performance .NET Wrapper for ICP DAS Hardware

![Version](https://img.shields.io/badge/Version-2.0.0-blue.svg?style=flat-square)![Platform](https://img.shields.io/badge/Platform-WinForms-lightgrey.svg?style=flat-square)![Driver](https://img.shields.io/badge/Driver-UniDAQ-orange.svg?style=flat-square)

**XUniDaq** is an advanced, event-driven wrapper specifically designed to bridge the gap between **ICP DAS Hardware (UniDAQ)** and modern **.NET WinForms** applications. It solves common pain points like thread safety, UI freezing, and graceful application shutdown.

---

## 🌟 Key Capabilities

| Feature | Description |
| :--- | :--- |
| **⚡ Auto-Initialization** | No manual startup code needed. The component detects `Runtime` mode and initializes the driver automatically via `EndInit()`. |
| **🛡️ Safe Shutdown** | Implements a strict `Stop()` and `Dispose` pattern to prevent `AccessViolationException` during form closing. |
| **🧵 Thread-Safe Events** | Automatically marshals data from high-speed background polling threads to the UI thread using `SynchronizationContext`. |
| **👁️ UI Watchdogs** | Custom controls (`AnalogMonitor`, `DigitalMonitor`) visually indicate connection loss (turn Gray) if data flow stops for >200ms. |

---

## 🏗️ Architecture Overview

The system uses a **Service Locator Pattern** to manage hardware resources efficiently:

1.  **`DaqServiceLocator`**: Holds the singleton instance of the driver manager.
2.  **`DaqSystemManager`**: Handles the raw `UniDAQ.dll` P/Invoke calls and runs the background polling loop.
3.  **`XUniDaq` (The Control)**: Acts as the client interface. It subscribes to the manager and exposes clean .NET events.

> **Note:** This architecture allows multiple UI controls to share the same physical hardware connection without conflict.

---

## 🚀 Getting Started

### 1. Integration in Visual Studio
*   **Step 1:** Add the `XUniDaq.dll` to your Toolbox.
*   **Step 2:** Drag the `XUniDaq` control onto your Form.
*   **Step 3:** Use the **Property Grid** to select your Board Index and configure Channels (AI/DI/DO).

### 2. Handling Data (The Right Way)

Since events are already on the UI thread, you can update controls directly:
```csharp
private void xUniDaq1_AiDataReceived(object sender, AnalogMultiChannelDataEventArgs e)
{
// SAFETY GUARD: Prevent updates if form is closing
if (this.IsDisposed) return;

// e.Data is a List<double[]> containing samples for each channel
var ch0_Value = e.Data[0].Last(); 

// Update the Smart Monitor Control
analogMonitorControl1.UpdateData(ch0_Value);
}
```
### 3. Implementing Control Logic

For interactive Digital Outputs, wire up the `WriteRequested` event:

```csharp
private void digitalOutputControl1_WriteRequested(object sender, DigitalWriteEventArgs e)
{
// Send command to hardware
xUniDaq1.WriteDigitalPort(e.ChannelName, e.NewState);
}
```

### ⚠️ Critical Implementation Patterns

To ensure your application is robust and crash-proof, strictly follow these patterns.

### 🛑 The Safe Shutdown Pattern
Closing a form while the driver is polling will cause a crash. You must unsubscribe and stop explicitly.

```csharp
protected override void OnFormClosing(FormClosingEventArgs e)
{
// 1. Unsubscribe to sever the link between Hardware and UI
xUniDaq1.AiDataReceived -= xUniDaq1_AiDataReceived;
xUniDaq1.StatusUpdated -= xUniDaq1_StatusUpdated;

// 2. Force stop the acquisition loop
xUniDaq1.Stop();

base.OnFormClosing(e);
}
```
### 📡 Watchdog Logic (Built-in)
The included UI controls (`AnalogMonitorControl`, `DigitalMonitorControl`) have internal timers:
*   **Active:** Updates visual state/text.
*   **Timeout (>200ms):** Sets text to `0.000` and Color to `Color.Gray`.
*   **Benefit:** Instantly alerts the operator if the software freezes or the cable is disconnected.

---

## 🔧 Advanced Configuration

### Custom Type Descriptor
The `XUniDaq` control implements `ICustomTypeDescriptor`. This means the Property Grid is **dynamic**:
*   If you select a **Digital-Only Board**, the `AiChannels` property will automatically disappear.
*   This prevents configuration errors at design time.

### Read-Only Properties
*   **`BoardName`**: Exposed as a read-only property in the designer. It fetches the actual model name (e.g., "PCI-1802") from the driver after initialization.

---

## 📝 Requirements
*   Visual Studio 2019+
*   .NET Framework 4.7.2 or 4.8
*   ICP DAS UniDAQ Driver installed on the host machine.
