# acer-battery-control

A small Windows CLI tool for controlling Acer battery features without installing Acer software. Tested on an Acer Swift 3 (SF314-512).

You may need to run it as Administrator.

## Features

* Battery health mode (80% charge limit)
* Battery calibration mode
* Battery temperature

## Usage

```text
acer-battery status
acer-battery temperature
acer-battery health on|off
acer-battery calibration on|off
```

## Credits

Inspired by [frederik-h/acer-wmi-battery](https://github.com/frederik-h/acer-wmi-battery), which documents Acer's battery WMI interface.
