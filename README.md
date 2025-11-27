# XUniDaq Enterprise Framework
### High-Performance .NET Abstraction Layer for ICP DAS Hardware

![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg?style=flat-square)![Version](https://img.shields.io/badge/version-1.0.0-blue.svg?style=flat-square)![Platform](https://img.shields.io/badge/platform-WinForms-lightgrey.svg?style=flat-square)![License](https://img.shields.io/badge/license-Apache--2.0-green.svg?style=flat-square)![Driver Support](https://img.shields.io/badge/driver-UniDAQ_DLL-orange.svg?style=flat-square)

---

## 📖 Executive Summary

**XUniDaq** is a comprehensive, industrial-grade middleware designed to bridge the gap between raw **ICP DAS C++ Drivers** and modern **.NET Applications**. It eliminates the complexity of unmanaged code interop, memory pointer management, and manual thread synchronization.

Built for **Mission-Critical Data Acquisition**, XUniDaq ensures robust stability through automated resource management, "Safe-Shutdown" protocols, and real-time signal processing (DSP) capabilities embedded directly within the driver wrapper.

---

## ⚡ Key Features & Architecture

### 1. Core Stability & Lifecycle Management
*   **Smart Initialization:** Leveraging `System.ComponentModel.ISupportInitialize`, the framework detects the runtime context. It automatically triggers `InitializeAndStartAsync()` in a Fire-and-Forget pattern via `EndInit()`, removing the need for manual startup code in the form constructor.
*   **Defense-in-Depth Shutdown:** Implements a rigid disposal pattern to prevent the common `AccessViolationException` (Crash on Exit) associated with hardware drivers.
*   **Watchdog UI Integration:** Controls like `DigitalOutputControl` and `AnalogMonitor` automatically disable/gray-out when the hardware link is severed.

### 2. Advanced Signal Processing (DSP)
Data is processed *before* it reaches the UI thread, ensuring performance:
*   **Linear Regression Engine:** Apply `y = ax + b` calibration per channel in real-time.
*   **Noise Reduction:** Configurable `SimpleMovingAverage` (SMA) filter windows.
*   **Matrix Data Marshaling:** Analog data is delivered as a 2D Matrix (`float[Samples, Channels]`), optimized for plotting libraries.

### 3. Interactive Control Systems
*   **Digital Toggle Logic:** Interactive `DigitalOutputControl` handles click events to toggle relays, bypassing the read-only nature of standard monitors.
*   **Async Command Queue:** Uses `WriteRequested` events to queue hardware commands without blocking the UI thread.

---

## 🛠️ Integration Guide

### Prerequisites
*   **Hardware:** ICP DAS PCI/PCIe Data Acquisition Boards.
*   **Driver:** UniDAQ Driver (installed on Host OS).
*   **Framework:** .NET Framework 4.7.2+.

### Installation
1.  Add references to **`XUniDaq.dll`**.
2.  Drag the `XUniDaq` component from the Toolbox onto your WinForms Designer.
3.  Configure the `BoardIndex` and `PollingInterval` in the Properties window.

---

## 💻 API Reference: Data Models (`XModels`)

The framework relies on strong typing provided by the `XModels` library to ensure compile-time safety.

### 1. `AnalogMultiChannelDataEventArgs`
The payload delivered by `AiDataReceived`. It is optimized to minimize garbage collection pressure.

| Property | Type | Description |
| :--- | :--- | :--- |
| `DataMatrix` | `float[,]` | The calibrated, filtered data. Indexed by `[SampleIndex, ChannelIndex]`. |
| `RawDataMatrix` | `float[,]` | The raw voltage values directly from the ADC, before Regression/Offset. |
| `Timestamp` | `DateTime` | Precise timestamp of when the buffer was filled. |
| `SampleCount` | `int` | Number of samples per channel in this packet. |

**Helper Methods:**
*   `GetChannelData(string channelName)`: Returns a flattened array for a specific channel (useful for Charts).
*   `GetValue(int sampleIndex, int channelIndex)`: Fast accessor for specific data points.

### 2. `VoltageRange` (Enum)
Maps readable names to internal driver Hex codes.
```csharp
public enum VoltageRange
{
Bipolar_10V = 0x1,      // +/- 10V
Bipolar_5V = 0x2,       // +/- 5V
Unipolar_10V = 0x3,     // 0V ~ 10V
Unipolar_20mA = 0x7     // 0 ~ 20mA (Requires Shunt Resistor)
}
```
---

## 🛡️ The "Safe Shutdown" Pattern (Critical)

To guarantee application stability, you **must** strictly follow this pattern in your Form's closing event. Failing to do so may cause the application to crash or hang due to unmanaged driver threads attempting to callback into a disposed UI.

```csharp
protected override void OnFormClosing(FormClosingEventArgs e)
{
// STEP 1: Unhook Event Handlers
// This prevents the driver from calling methods on disposed controls.
xUniDaq1.AiDataReceived -= xUniDaq1_AiDataReceived;
xUniDaq1.DiDataReceived -= xUniDaq1_DiDataReceived;
xUniDaq1.StatusUpdated -= xUniDaq1_StatusUpdated;

// STEP 2: Halt Hardware
// Sends the STOP command to the C++ driver.
xUniDaq1.Stop(); 

// STEP 3: Dispose
// Cleans up unmanaged pointers.
xUniDaq1.Dispose();

base.OnFormClosing(e);
}
```
---

## 💡 Code Examples

### Scenario A: Handling High-Speed Analog Data
This example demonstrates how to extract the latest value for a specific channel safely.

```csharp
private void xUniDaq1_AiDataReceived(object sender, AnalogMultiChannelDataEventArgs e)
{
// 1. Thread-Safety Guard
if (this.IsDisposed || !this.Visible) return;

try 
{
// 2. Extract Data for "Pressure_Sensor_1" (Channel 0)
// The matrix allows bulk access. Here we just take the last sample.
int channelIndex = 0; 
int lastSampleIndex = e.SampleCount - 1;

float value = e.DataMatrix[lastSampleIndex, channelIndex];

// 3. Update UI
analogMonitor1.UpdateData(value);
}
catch (Exception ex)
{
// Log error without crashing the acquisition thread
Console.WriteLine($"Data Error: {ex.Message}");
}
}
```
### Scenario B: Configuring Calibration at Runtime
You can adjust the signal processing parameters on the fly without stopping the board.

```csharp
public void CalibrateSensor(string channelName, float actualValue, float rawValue)
{
// Calculate linear regression (y = mx + b)
// This logic is handled internally by XUniDaq once parameters are set.

double slope = 1.05; // Example calculation
double offset = -0.2; // Example calculation

// Apply to XUniDaq Engine
xUniDaq1.UpdateRegression(channelName, new double[] { offset, slope });

// Apply Smoothing (Moving Average of 10 samples)
xUniDaq1.UpdateFilterWindow(channelName, 10);
}
```
---

## 🐛 Troubleshooting & Error Codes (`UniDaqError`)

The library automatically converts obscure C++ error codes into `XModels.UniDaqError` enums and human-readable messages.

*   **Error 0 (`NoError`):** Operation successful.
*   **Error 6 (`BoardNotFound`):** Check PCI slot or Driver Installation.
*   **Error 33 (`FifoOverflow`):** Polling rate is too slow for the sampling frequency. Decrease `PollingInterval` or reduce Channel count.

---

<div align="center">
  <sub>Example Project © 2025 | Designed for High-Reliability Industrial Control Systems</sub>
</div>
