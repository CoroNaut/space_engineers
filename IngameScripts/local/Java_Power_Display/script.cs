/*
 *   R e a d m e
 *   -----------
 * 
 *   In this file you can include any instructions or other comments you want to have injected onto the 
 *   top of your final script. You can safely delete this file if you do not want any such comments.
 * 
 */

/*
        Created by JavaSkeptre
        To monitor all solar panels, batteries and reactors power and power usage.
        Edit Variables inside next brackets
        */

//Start to change variables

private string displayNameForLCD = "LCD Java_Power";

//Stop Changing variables

private static long calls = 0;
private static int version = 11;
private IMyShipController control;
private bool init = false;
private static string initialArgs;

private int blockNum;
private IMyTextPanel display;
private List<IMyTerminalBlock>[] blockData;
private readonly bool gridSize;

public Program() {
    Runtime.UpdateFrequency=UpdateFrequency.Update100;
    gridSize=Me.CubeGrid.GridSizeEnum.ToString()=="Large";
    Init();
}
private void Init() {
    initialArgs="Checking";
    List<IMyShipController> controllers = new List<IMyShipController> { };
    GridTerminalSystem.GetBlocksOfType<IMyShipController>(controllers);
    try {
        control=controllers[0];
    } catch {
        Echo($"No ship controller has not been found");
        return;
    }
    try {
        display=(IMyTextPanel)GridTerminalSystem.GetBlockWithName(displayNameForLCD);
    } catch {
        Echo($"LCD Panel with name: {displayNameForLCD}\nhas not been found");
        return;
    }
    if(display.Enabled==false) {
        Echo($"LCD Panel with name: {displayNameForLCD}\nis turned off");
        return;
    }
    blockData=new List<IMyTerminalBlock>[5];
    for(int x = 0; x<blockData.Length; x++) {
        blockData[x]=new List<IMyTerminalBlock>();
    }
    GridTerminalSystem.GetBlocksOfType<IMySolarPanel>(blockData[0]);//Solar panels
    GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(blockData[1]);//Batteries
    GridTerminalSystem.GetBlocksOfType<IMyReactor>(blockData[2]);//Reactors
    GridTerminalSystem.GetBlocksOfType<IMyGasTank>(blockData[3]);//Tanks
    GridTerminalSystem.GetBlocksOfType<IMyJumpDrive>(blockData[4]);//JumpDrives
    List<IMyFunctionalBlock> blocks = new List<IMyFunctionalBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyFunctionalBlock>(blocks);
    blockNum=blocks.Count;
    this.init=true;
    initialArgs="Running";
}
private void Main(string argument, UpdateType updateSource) {
    DateTime dt = DateTime.Now;
    Echo($"Calls since compile: {calls++}\nVersion: {version} Update:{updateSource.ToString()}{(initialArgs!=null ? ($"\nRunning: {initialArgs}") : "")}\n");
    List<IMyFunctionalBlock> blocks = new List<IMyFunctionalBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyFunctionalBlock>(blocks);
    if(!init||display==null||control==null||blockData==null||blocks.Count!=blockNum) {
        Init();
    }
    Run(argument);
    Echo($"\nRuntime: {(DateTime.Now-dt).TotalMilliseconds.ToString("0.000000")} ms");
    Echo($"Current Load: {(Runtime.CurrentInstructionCount/Runtime.MaxInstructionCount*100.0f).ToString("0.00")} %");
    Echo($"Instructions: {Runtime.CurrentInstructionCount.ToString()}");
    Echo("Successful Completion");
}
private string Run_Solar() {
    if(blockData[0].Count==0) {
        return "";
    }
    int solarFunctional = 0;
    double solarCurrent = 0;//MW
    double solarMax = 0;//MW
    double solarTotal = gridSize ? blockData[0].Count*120/1000d : blockData[0].Count*30/1000;
    foreach(IMySolarPanel solar in blockData[0]) {
        if(solar.IsFunctional==true) {
            solarCurrent+=solar.CurrentOutput;
            solarMax+=solar.MaxOutput;
            solarFunctional++;
        }
    }
    //string ret = ThreeBar(solarCurrent, solarMax, solarTotal);
    string ret2 = ThreeBar(new Vector3D(solarCurrent, solarMax, solarTotal));
    return (blockData[0].Count==solarFunctional ? $"({solarFunctional}" : $"({solarFunctional}/{blockData[0].Count}")+$") Solar: {FormatNum(solarCurrent)}/ {FormatNum(solarMax)}/ {FormatNum(solarTotal)}\n{ret2}\n\n";
}
private string Run_Battery() {
    if(blockData[1].Count==0) {
        return "";
    }
    int batteryFunctional = 0;
    double batteryStoredMax = 0;//MWH
    double batteryStoredCur = 0;//MWH
    double batteryOutput = 0;//MW
    double batteryInput = 0;//MW
    foreach(IMyBatteryBlock battery in blockData[1]) {
        if(battery.IsFunctional==true) {
            batteryStoredMax+=battery.MaxStoredPower;
            batteryStoredCur+=battery.CurrentStoredPower;
            batteryOutput+=battery.CurrentOutput;
            batteryInput+=battery.CurrentInput;
            batteryFunctional++;
        }
    }
    return (blockData[1].Count==batteryFunctional ? $"({batteryFunctional}" : $"({batteryFunctional}/{blockData[1].Count}")+$") Battery: {FormatNum(batteryStoredCur)}h / {FormatNum(batteryStoredMax)}h\nIN: {FormatNum(batteryInput)} OUT: {FormatNum(batteryOutput)}\n{BarBuilder(batteryStoredCur/batteryStoredMax)}\n\n";
}
private string Run_Reactor() {
    if(blockData[2].Count==0) {
        return "";
    }
    int reactorFunctional = 0;
    double reactorCurrent = 0;//MW
    double reactorMax = 0;//MW
    double uraniumTotal = 0;
    foreach(IMyReactor reactor in blockData[2]) {
        if(reactor.IsFunctional==true) {
            var reactorItem = reactor.GetInventory(0).GetItemAt(0);
            if(reactorItem!=null){
                reactorCurrent+=reactor.CurrentOutput;
                reactorMax+=reactor.MaxOutput;
                uraniumTotal+=(double)(((MyInventoryItem)reactorItem).Amount.ToIntSafe());
                reactorFunctional++;
            }
        }
    }
    TimeSpan powerTime = new TimeSpan(0, 0, (int)((reactorCurrent==0d ? 0d : 1d/reactorCurrent)*3600d*uraniumTotal));
    return (blockData[2].Count==reactorFunctional ? "("+reactorFunctional : "("+reactorFunctional+"/"+blockData[2].Count)+$") Reactor Uranium: {uraniumTotal.ToString("0.00000")}\nOutput: {FormatNum(reactorCurrent)}/{FormatNum(reactorMax)}\n{BarBuilder(reactorCurrent/reactorMax)}\nPower Time: {(powerTime.Equals(new TimeSpan(0, 0, 0)) ? "---" : powerTime.ToString())}\n\n";
}
private string Run_GasFillLevel() {
    if(blockData[3].Count==0) {
        return "";
    }
    double hydrogenCurrent = 0;
    int hydroContainers = 0;
    double oxygenCurrent = 0;
    int oxygenContainers = 0;
    if(blockData[3].Count>0) {
        foreach(IMyGasTank tank in blockData[3]) {
            string[] info = tank.DetailedInfo.Split(':');
            if(tank.DetailedInfo.IndexOf("Oxygen")==-1) {
                hydrogenCurrent+=double.Parse(info[3].Substring(0, info[3].IndexOf("%")));
                hydroContainers++;
            } else {
                oxygenCurrent+=double.Parse(info[3].Substring(0, info[3].IndexOf("%")));
                oxygenContainers++;
            }
        }
    }
    return (oxygenContainers==0 ? "" : $"({oxygenContainers}) Oxygen: {(oxygenCurrent/oxygenContainers).ToString("0.00")}%\n")+(hydroContainers==0 ? "" : $"({hydroContainers}) Hydrogen: {(hydrogenCurrent/hydroContainers).ToString("0.00")}%\n");
}
private string Run_JumpCalc() {
    if(blockData[4].Count==0) {
        return "";
    }
    double maxJumpCharge = 0;
    if(blockData[4].Count>0) {
        foreach(IMyJumpDrive drive in blockData[4]) {
            if(drive.CurrentStoredPower>maxJumpCharge) {
                maxJumpCharge+=drive.CurrentStoredPower;
            }
        }
        maxJumpCharge=maxJumpCharge/3*100;
    }
    return $"({blockData[4].Count}) Jump: {maxJumpCharge.ToString("0.00")}%, ";
}
private string Run_Gravity() {
    Vector3 gravity = control.GetNaturalGravity();
    return gravity.Equals(new Vector3(0, 0, 0)) ? "\n" : $"Gravity: {gravity.Length().ToString("0.00")}m/s\n";
}
private string Run_MassCalc() {
    double totalMass = control.CalculateShipMass().TotalMass;
    double baseMass = control.CalculateShipMass().BaseMass;
    double cargoMass = totalMass-baseMass;
    return baseMass!=0 ? $"Ship Mass: {baseMass} kg\nCargo Mass: {cargoMass} kg\nTotal Mass: {totalMass}kg\n" : "Ship is station\n";
}
private void Run(string argument) {
    string retVal = "";
    retVal+=Run_JumpCalc();
    retVal+=Run_Gravity();
    retVal+=Run_Solar();
    retVal+=Run_Battery();
    retVal+=Run_Reactor();
    retVal+=Run_GasFillLevel();
    retVal+=Run_MassCalc();

    Me.CustomData=retVal;
    display.WriteText(retVal);
}
public string ThreeBar(double one, double two, double three) {
    StringBuilder threeBarString = new StringBuilder();
    threeBarString.Append("[");
    int i;
    for(i=0; i<(one/two*(two/three)*100/2); i++) {
        threeBarString.Append("|");
    }
    if(one!=two) {
        threeBarString.Append("|");
    }
    int j;
    for(j=i; j<(two/three*100/2); j++) {
        threeBarString.Append("`");
    }
    threeBarString.Append("|");
    for(int k = j; k<50; k++) {
        threeBarString.Append("`");
    }
    threeBarString.Append("]");
    if(one==0||two==0) {
        threeBarString.Append(" 0 %");
    } else {
        threeBarString.Append($" {(one/two).ToString("0.00"+" %")}");
    }
    string output = threeBarString.ToString();
    threeBarString.Clear();
    return output;
}
public static string ThreeBar(Vector3D vec) {
    vec.Normalize();
    StringBuilder threeBarString = new StringBuilder();
    threeBarString.Append("[");
    int i;
    double v = vec.X/vec.Y;
    double v1 = vec.Y/vec.Z;
    for(i=0; i<v*v1*49; i++) {
        threeBarString.Append("|");
    }
    int j;
    for(j=i; j<v1*49; j++) {
        threeBarString.Append("`");
    }
    //if(vec.X!=vec.Y&&vec.X!=0.0) {
    //    threeBarString.Append("|");
    //} else {
    //    threeBarString.Append(".");
    //}
    int k;
    for(k=j; k<49; k++) {
        threeBarString.Append("`");
    }
    threeBarString.Append("]");
    threeBarString.AppendFormat(" {0,8:P2}", ((vec.X)/(vec.Y==0 ? 1 : vec.Y)));
    return threeBarString.ToString();
}
public string BarBuilder(double num) {
    StringBuilder barString = new StringBuilder();
    barString.Append("[");
    int i;
    double p = num*100;
    for(i=0; i<(p/2); i++) {
        barString.Append("|");
    }
    int l = 50-i;
    while(l>0) {
        barString.Append("`");
        l--;
    }
    barString.Append($"] {(p/100).ToString("0.00"+" %")}");
    string barOutput = barString.ToString();
    barString.Clear();
    return barOutput;
}
public string FormatNum(double number) {
    string ordinals = " kMGTPEZY";
    double compressed = number*1000000d;
    int start = ordinals.IndexOf(' ');
    int ordinal = (start<0 ? 0 : start);
    while(compressed>=1000&&ordinal+1<ordinals.Length) {
        compressed/=1000;
        ordinal++;
    }
    string res = Math.Round(compressed, 1, MidpointRounding.AwayFromZero).ToString();
    if(ordinal>0) {
        return $"{res} {ordinals[ordinal]}W";
    }
    return $"{res}W";
}