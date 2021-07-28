/*
        Created by JavaSkeptre
        To use a group of thrusters to ascend past the gravity well.
        Your arguments should have the following format:
        Ex) "A1H2I3" for atmospheric first, hydrogen second, and ion third
        Ex) "A2H3I1" for ion first, atmospheric second, hydrogen third
        Ex) "A0H0I3" for not using atmospheric or hydrogen, and only using ions
        Ex) "Stop" to halt the current ascent
        Furthermore, chance the below "updateTime" variable to 1,10,or 100,
            any other number will result in updateTime=10
        */

private const int updateTime = 1;

// DO NOT EDIT BELOW THIS LINE

private static long calls = 0;
private static readonly int version = 8;
private static IMyShipController control;
private bool init = false, stopped = true;
private static string initialArgs;

private static readonly int[] largeThrustVals = { 5400000, 420000, 6000000, 900000, 3600000, 288000 };
private static readonly int[] smallThrustVals = { 408000, 80000, 400000, 82000, 144000, 12000 };
private static readonly double[] largePowerReqVals = { 16.360000, 2.360000, 0.0, 0.0, 33.600000, 3.360000 };
private static readonly double[] smallPowerReqVals = { 2.400000, 0.701000, 0.0, 0.0, 2.400000, 0.201000 };
private static readonly string[] descriptors = { "Large Atmospheric", "Atmospheric", "Large Hydrogen", "Hydrogen", "Large Ion", "Ion" };

private int blockNum;
private readonly int hydroCapacity;
private readonly int hydroGeneration;
private readonly bool gridSize;
private readonly int[] setThrustVals;
private readonly double[] setPowerReqVals;
private static List<int> ahiPriorities = new List<int>();
private static List<IMyThrust>[] tList = new List<IMyThrust>[6];
private static List<IMyReactor> reactors = new List<IMyReactor>();
private static IMyGyro gyro;

//private static readonly string regexForCorrectAgument = "(?:A([1-3]{1}))(?:H((?!\1)[1-3]{1}))(?:I(?!\2)[1-3]{1})";
public Program() {
    Runtime.UpdateFrequency=UpdateFrequency.None;
    gridSize=Me.CubeGrid.GridSizeEnum.ToString()=="Large" ? true : false;
    hydroCapacity=gridSize ? 2500000 : 80000;
    hydroGeneration=gridSize ? 1670 : 830;
    setThrustVals=gridSize ? largeThrustVals : smallThrustVals;
    setPowerReqVals=gridSize ? largePowerReqVals : smallPowerReqVals;
    Init();
}
private void Init() {
    List<IMyShipController> controllers = new List<IMyShipController>();
    GridTerminalSystem.GetBlocksOfType<IMyShipController>(controllers);
    List<IMyGyro> gyroscopes = new List<IMyGyro>();
    GridTerminalSystem.GetBlocksOfType<IMyGyro>(gyroscopes);
    try {
        control=controllers[0];
        gyro=gyroscopes[0];
    } catch(NullReferenceException) {
        Echo($"No ship controller has not been found");
        return;
    }
    List<IMyThrust> thrusters = new List<IMyThrust>();
    GridTerminalSystem.GetBlocksOfType<IMyThrust>(thrusters);
    if(thrusters.Count==0) {
        Echo($"There are no down thrusters\nAdd thrusters to the group\nStopping script before running");
        return;
    }
    for(int x = 0; x<thrusters.Count; x++) {
        if(thrusters[x].Orientation.ToString().IndexOf("Down")!=9) {
            thrusters.Remove(thrusters[x--]);
        }
    }
    for(int x = 0; x<tList.Length; x++) {
        tList[x]=new List<IMyThrust>();
    }
    foreach(IMyThrust thruster in thrusters) {
        for(int x = 0; x<tList.Length; x++) {
            if(thruster.DetailedInfo.IndexOf(descriptors[x]+" Thruster"+(x%2==0 ? "" : "s"))!=-1) {
                tList[x].Add(thruster);
                break;
            }
        }
    }
    GridTerminalSystem.GetBlocksOfType<IMyReactor>(reactors);
    List<IMyFunctionalBlock> blocks = new List<IMyFunctionalBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyFunctionalBlock>(blocks);
    blockNum=blocks.Count;
    init=true;
}
private void TerminateAscent() {
    for(int x = 0; x<tList.Length; x++) {
        foreach(IMyThrust t in tList[x]) {
            t.ThrustOverridePercentage=0f;
        }
    }
    initialArgs=null;
    stopped=true;
    control.DampenersOverride=true;
    Runtime.UpdateFrequency=UpdateFrequency.Once;
}
private bool Run_VerifyStart(string argument) {
    if((stopped&&argument.Equals(""))||argument.Equals("Stop")) {
        Echo("Auto Ascent is stopped");
        if(!stopped) {
            TerminateAscent();
        }
        return false;
    }
    if(initialArgs==null||(!argument.Equals("")&&!initialArgs.Equals(argument))) {
        char[] args = argument.ToCharArray();
        if(args.Length==6&&args[0]=='A'&&args[2]=='H'&&args[4]=='I') {
            try {
                ahiPriorities.Clear();
                ahiPriorities.Add(int.Parse(args[1].ToString()));
                ahiPriorities.Add(int.Parse(args[3].ToString()));
                ahiPriorities.Add(int.Parse(args[5].ToString()));
            } catch {
                Echo("Program arguments' priorities aren't numbers.\nStopping script before running");
                return false;
            }
            if(ahiPriorities[0]==ahiPriorities[1]||ahiPriorities[0]==ahiPriorities[2]||ahiPriorities[1]==ahiPriorities[2]) {
                Echo("Program arguments cannot have similar priorities.\nStopping script before running");
                return false;
            }
            for(int i = 0; i<=2; i++) {
                if(ahiPriorities[i]>3||ahiPriorities[i]<0) {
                    Echo("Program arguments' priorities aren't within [0,1,2,3].\nStopping script before running");
                    return false;
                }
            }
        } else {
            Echo("Program arguments aren't correct.\nStopping script before running");
            return false;
        }
        initialArgs=argument;
    }
    if(control.GetNaturalGravity().Equals(new Vector3(0, 0, 0))) {
        Echo("No gravity to escape\nResetting overrides\nStopping script before running");
        TerminateAscent();
        return false;
    }
    return true;
}
public void Main(string argument, UpdateType updateSource) {
    DateTime dt = DateTime.Now;
    Echo($"Calls since compile: {calls++}\nVersion: {version} Update:{updateSource.ToString()}{(initialArgs!=null ? ($"\nRunning: {initialArgs}") : "")}\n");
    List<IMyFunctionalBlock> blocks = new List<IMyFunctionalBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyFunctionalBlock>(blocks);
    if(!init||control==null||tList==null||reactors==null||gyro==null||blocks.Count!=blockNum) {
        Init();
    }
    Run(argument);
    Echo($"\nRuntime: {(DateTime.Now-dt).TotalMilliseconds.ToString("0.000000")} ms");
    Echo($"Current Load: {(Runtime.CurrentInstructionCount/Runtime.MaxInstructionCount*100.0f).ToString("0.00")} %");
    Echo($"Instructions: {Runtime.CurrentInstructionCount.ToString()}");
    Echo("Successful Completion");
}
private void Run(string argument) {
    if(!Run_VerifyStart(argument)) {
        return;
    }
    stopped=false;
    Runtime.UpdateFrequency=updateTime==1 ? UpdateFrequency.Update1 : updateTime==100 ? UpdateFrequency.Update100 : UpdateFrequency.Update10;
    control.DampenersOverride=false;
    double currentAltitude = 0;
    control.TryGetPlanetElevation(Sandbox.ModAPI.Ingame.MyPlanetElevation.Sealevel, out currentAltitude);
    double atmosAltMultiplier = Math.Max(0d, 1-(currentAltitude/10000));
    double ionAltMultiplier = Math.Min(1d, Math.Max(.3d, currentAltitude/20000d*.7d));
    double[] tAmt = new double[6];
    tAmt[0]=setThrustVals[0]*atmosAltMultiplier;
    tAmt[1]=setThrustVals[1]*atmosAltMultiplier;
    tAmt[2]=setThrustVals[2];
    tAmt[3]=setThrustVals[3];
    tAmt[4]=setThrustVals[4]*ionAltMultiplier;
    tAmt[5]=setThrustVals[5]*ionAltMultiplier;
    double maxThrust = 0;
    for(int x = 0; x<=5; x++) {
        maxThrust+=tAmt[x]*tList[x].Count;
    }
    double grav = control.GetNaturalGravity().Length();
    double totalMass = control.CalculateShipMass().TotalMass;
    double maxAccel = (maxThrust/totalMass)-grav;
    double minimumThrust = (grav*totalMass)+0.01d;
    if(maxAccel<=0.01d) {
        Echo($"{maxThrust.ToString("0.00")}/{minimumThrust.ToString("0.00")} thrust required\n");
        Echo("Ship does not have enough thrust to lift off\nStopping script before running");
        return;
    } else if(maxAccel<.2d) {
        Echo("Excercise caution!\nShip has a small max acceleration!");
    }
    double currentSpeed = control.GetShipSpeed();
    Echo($"Speed: {currentSpeed.ToString("0.00")}");
    Echo($"Altitude: {currentAltitude.ToString("0.00")}\n");
    double wantedThrust = minimumThrust*Math.Pow(.9997d+((100d-currentSpeed)/200d), 3);
    double reactorCurrent = 0;//MW
    double reactorMax = 0;//MW
    if(reactors.Count>0) {
        foreach(IMyReactor reactor in reactors) {
            if(reactor.IsFunctional==true) {
                reactorCurrent+=reactor.CurrentOutput;
                reactorMax+=reactor.MaxOutput;
            }
        }
    }
    for(int x = 0; x<tList.Length; x++) {
        if(tList[x].Count>0) {
            reactorCurrent-=setPowerReqVals[x]*tList[x].Count*tList[x][0].ThrustOverridePercentage;
        }
    }
    double powerUseAllowed = (reactorMax-reactorCurrent)-0.100d;//leave 100kW extra
    for(int x = 0; x<=2; x++) {
        if(ahiPriorities[x]!=0) {
            for(int c = 0; c<=1; c++) {
                int offset = (ahiPriorities.IndexOf(x+1)*2)+c;
                if(tList[offset].Count>0) {
                    if(tAmt[offset]*tList[offset].Count>0) {
                        if(powerUseAllowed==0.0d&&setPowerReqVals[offset]>0.0d) {
                            break;
                        }
                        double overrideRatio = Math.Min(wantedThrust/(tAmt[offset]*tList[offset].Count), 1);
                        double powerUse = overrideRatio*tList[offset].Count*setPowerReqVals[offset];
                        if(powerUse>powerUseAllowed&&powerUse!=0.0) {
                            double rat = powerUseAllowed/powerUse;
                            overrideRatio*=rat;
                            powerUseAllowed=0.0d;
                            Echo("Causing Low power problem:");
                        } else {
                            powerUseAllowed-=powerUse;
                        }
                        Echo(descriptors[offset]+$": {(overrideRatio*100).ToString("0.00")}%");
                        wantedThrust-=tList[offset].Count*overrideRatio*tAmt[offset];
                        foreach(IMyThrust t in tList[offset]) {
                            t.ThrustOverridePercentage=(float)overrideRatio;
                        }
                    } else {
                        double cur = tList[offset][0].ThrustOverride;
                        if(cur>0) {
                            foreach(IMyThrust t in tList[offset]) {
                                t.ThrustOverridePercentage=0.0f;
                            }
                        }
                    }
                }
            }
        }
    }
}