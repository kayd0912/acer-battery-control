using System.Management;

if (args.Length == 0)
{
    PrintHelp();
    return;
}

try {
    var scope = new ManagementScope(@"\\.\root\wmi");
    scope.Connect();

    using var battery = GetBatteryControl(scope);

    switch (args[0].ToLower())
    {
        case "status":
            GetBatteryHealthStatus(battery);
            break;

        case "temperature":
            GetBatteryTemperature(battery);
            break;

        case "health":
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: acer-battery health <on|off>");
                return;
            }

            SetBatteryHealth(battery, ParseEnabled(args[1]));
            break;

        case "calibration":
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: acer-battery calibration <on|off>");
                return;
            }

            SetBatteryCalibration(battery, ParseEnabled(args[1]));
            break;

        default:
            PrintHelp();
            break;
    }
} 
catch (ManagementException ex) when (ex.ErrorCode == ManagementStatus.AccessDenied) {
    Console.Error.WriteLine("Error: Access denied. Try running as administrator.");
}
catch (ArgumentException ex) {
    Console.Error.WriteLine($"Error: {ex.Message}");
}

static ManagementObject GetBatteryControl(ManagementScope scope)
{
    using var searcher = new ManagementObjectSearcher(
        scope,
        new ObjectQuery("SELECT * FROM BatteryControl")
    );

    foreach (ManagementObject battery in searcher.Get())
    {
        return battery;
    }

    throw new Exception("Acer BatteryControl WMI interface not found.");
}


static void GetBatteryHealthStatus(ManagementObject battery)
{
    using ManagementBaseObject input =
        battery.GetMethodParameters("GetBatteryHealthControlStatus");

    input["uBatteryNo"] = (byte)1;
    input["uFunctionQuery"] = (byte)1;
    input["uReserved"] = new byte[] { 0, 0 };

    using ManagementBaseObject output =
        battery.InvokeMethod(
            "GetBatteryHealthControlStatus",
            input,
            null
        );

    byte[] functionStatus = (byte[])output["uFunctionStatus"];

    Console.WriteLine(
        $"Health Mode: {(functionStatus[0] > 0 ? "ON" : "OFF")}"
    );

    Console.WriteLine(
        $"Calibration: {(functionStatus[1] > 0 ? "ON" : "OFF")}"
    );
}

static void GetBatteryTemperature(ManagementObject battery)
{
    using ManagementBaseObject input =
        battery.GetMethodParameters("GetBattInfoInterface");

    input["uBatteryInfoIndex"] = (uint)0x8;
    input["uBatteryNo"] = (uint)1;

    using ManagementBaseObject output =
        battery.InvokeMethod(
            "GetBattInfoInterface",
            input,
            null
        );

    uint value = (uint)output["uReturn"];
    double temperature = (value - 2731) / 10.0;

    Console.WriteLine($"Temperature: {temperature:F1} °C");
}

static void SetBatteryFunction(
    ManagementObject battery,
    byte functionMask,
    bool enabled)
{
    using ManagementBaseObject input =
        battery.GetMethodParameters("SetBatteryHealthControl");

    input["uBatteryNo"] = (byte)1;
    input["uFunctionMask"] = functionMask;
    input["uFunctionStatus"] = enabled ? (byte)1 : (byte)0;
    input["uReservedIn"] = new byte[] { 0, 0, 0, 0, 0 };

    using ManagementBaseObject output =
        battery.InvokeMethod(
            "SetBatteryHealthControl",
            input,
            null
        );

    ushort uReturn = (ushort)output["uReturn"];

    if (uReturn != 0)
    {
        throw new Exception($"Acer returned error code: {uReturn}");
    }
}

static void SetBatteryHealth(
    ManagementObject battery,
    bool enabled)
{
    SetBatteryFunction(battery, 1, enabled);

    Console.WriteLine(
        $"Health Mode: {(enabled ? "ON" : "OFF")}"
    );
}

static void SetBatteryCalibration(
    ManagementObject battery,
    bool enabled)
{
    SetBatteryFunction(battery, 2, enabled);

    Console.WriteLine(
        $"Calibration: {(enabled ? "ON" : "OFF")}"
    );
}

static bool ParseEnabled(string value)
{
    if (value.Equals("on", StringComparison.OrdinalIgnoreCase))
        return true;

    if (value.Equals("off", StringComparison.OrdinalIgnoreCase))
        return false;

    throw new ArgumentException("Expected 'on' or 'off'.");
}

static void PrintHelp()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  acer-battery status");
    Console.WriteLine("  acer-battery temperature");
    Console.WriteLine("  acer-battery health <on|off>");
    Console.WriteLine("  acer-battery calibration <on|off>");
}