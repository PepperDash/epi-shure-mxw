# Essentials Shure MXW Wireless Microphone Plugin

## License

Provided under MIT license

## Overview

This plugin reports battery and charging status for Shure MXW Series wireless microphones

## Dependencies

The [Essentials](https://github.com/PepperDash/Essentials) libraries are required. They referenced via nuget. You must have nuget.exe installed and in the `PATH` environment variable to use the following command. Nuget.exe is available at [nuget.org](https://dist.nuget.org/win-x86-commandline/latest/nuget.exe).

### Installing Dependencies

Dependencies will be installed automatically by Visual Studio on opening. Use the Nuget Package Manager in
Visual Studio to manage nuget package dependencies. All files will be output to the `output` directory at the root of
repository.

### Installing Different versions of PepperDash Core

If a different version of PepperDash Core is needed, use the Visual Studio Nuget Package Manager to install the desired
version.

# Usage

## Join Map

### Digitals

| Join Number | Join Span | Description                    | Type                | Capabilities |
| ----------- | --------- | ------------------------------ | ------------------- | ------------ |
| 3901        | 1         | Device Online                  | Digital             | ToSIMPL      |
| 3902        | 1         | Mic Enabled                    | Digital             | ToSIMPL      |
| 3903        | 1         | Mic Low Battery Caution        | Digital             | ToSIMPL      |
| 3904        | 1         | Mic Low Battery Warning        | Digital             | ToSIMPL      |
| 3905        | 1         | Mic On Charger                 | Digital             | ToSIMPL      |
| 3906        | 1         | Mic On Charger Feedback Enable | Digital             | ToSIMPL      |
| 3948        | 1         | Any Button WasPressed          | Digital             | ToSIMPL      |

### Analogs

| Join Number | Join Span | Description               | Type                | Capabilities |
| ----------- | --------- | ------------------------- | ------------------- | ------------ |
| 3902        | 1         | Mic Status                | Analog              | ToSIMPL      |
| 3903        | 1         | Mic Battery level 0-100%  | Analog              | ToSIMPL      |
| 3904        | 1         | Mic Battery Status        | Analog              | ToSIMPL      |
| 3905        | 1         | Mic Battery level 0-65535 | Analog              | ToSIMPL      |

### Serials

| Join Number | Join Span | Description           | Type                | Capabilities |
| ----------- | --------- | --------------------- | ------------------- | ------------ |
| 3901        | 1         | Aggregate ErrorString | Serial              | ToSIMPL      |
| 3902        | 1         | Mic Name              | Serial              | ToSIMPL      |
| 3950        | 1         | Device Name Name      | Serial              | ToSIMPL      |


## Example Config
```json
{
  "key": "MicRx-1",
  "name": "ARX-01",
  "group": "api",
  "type": "shuremxw",
  "properties": {
      "control": {
          "method": "tcpIp",
          "tcpSshProperties": {
              "address": "192.168.0.231",
              "port": "2202",
              "autoReconnect": true,
              "autoReconnectIntervalMs": 5000
          }
      },
      "cautionThreshold": 50,
      "warningThreshold": 20,
      "mics": {
          "mic-01" : {
              "enabled": true,
              "index": 1,
              "name": "A-Mic 1"
          },
          "mic-02" :{
              "enabled": true,
              "index": 2,
              "name": "A-Mic 2"
          },
          "mic-03" :{
              "enabled": true,
              "index": 3,
              "name": "A-Mic 3"
          },
          "mic-04" :{
              "enabled": true,
              "index": 4,
              "name": "A-Mic 4"
          }
      }
  }
}
```

> note: Caution Threshold and Warning Threshold use the Percentage value to determine state