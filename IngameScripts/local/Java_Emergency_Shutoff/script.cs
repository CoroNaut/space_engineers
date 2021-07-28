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
        To turn off everything except essential blocks under a specific power time remaining
        Edit Variables inside next brackets
        */

//Start to change variables

private static int timeToShutOff = 2*60*60; //2hr*60min*60sec=7200sec
private const string onKey = "FORCE_ON";
private const string offKey = "FORCE_OFF";

//Stop Changing variables

private static long calls = 0;
private static int version = 11;
private IMyShipController control;
private bool init = false;
private static string initialArgs;

private static bool turnedOff = false;
private static int blockNum;
private List<IMyFunctionalBlock> blocks = new List<IMyFunctionalBlock>();

public Program() {
    Runtime.UpdateFrequency=UpdateFrequency.Update100;
    Init();
}
private void Init() {
    List<IMyShipController> controllers = new List<IMyShipController> { };
    GridTerminalSystem.GetBlocksOfType<IMyShipController>(controllers);
    try {
        control=controllers[0];
    } catch {
        Echo($"No ship controller has not been found");
        return;
    }
    GridTerminalSystem.GetBlocksOfType<IMyFunctionalBlock>(blocks);
    blockNum=blocks.Count;
    this.init=true;
}
private void Main(string argument, UpdateType updateSource) {
    DateTime dt = DateTime.Now;
    Echo($"Calls since compile: {calls++}\nVersion: {version} Update:{updateSource.ToString()}{(initialArgs!=null ? ($"\nRunning: {initialArgs}") : "")}\n");
    GridTerminalSystem.GetBlocksOfType<IMyFunctionalBlock>(blocks);
    if(!init||blockNum!=blocks.Count) {
        Init();
    }
    Run(argument);
    Echo($"\nRuntime: {(DateTime.Now-dt).TotalMilliseconds.ToString("0.000000")} ms");
    Echo($"Current Load: {(Runtime.CurrentInstructionCount/Runtime.MaxInstructionCount*100.0f).ToString("0.00")} %");
    Echo($"Instructions: {Runtime.CurrentInstructionCount.ToString()}");
    Echo("Successful Completion");
}
private void Run(string argument) {
    if(argument==onKey) {
        turnedOff=false;
        Echo("Turned On");
        initialArgs=onKey;
        if(Me.CustomData!="") {
            foreach(IMyFunctionalBlock block in blocks) {
                if(Me.CustomData.Contains(block.CustomName)) {
                    try {
                        block.ApplyAction("OnOff_On");
                    } catch {
                        Echo("Failed: "+block.CustomName);
                    }
                }
            }
        }
        Me.CustomData="";
    } else if(turnedOff) {
        Echo("EMERGENCY SYSTEM ACTIVATED");
        initialArgs="EMERGENCY SYSTEM ACTIVATED";
        foreach(IMyFunctionalBlock b in blocks) {
            if(!b.Enabled) {
                return;
            }
        }
        Echo("Emergency system detected all blocks are on\nRestored to normal state");
        turnedOff=false;
    }
    double powerTimeSec = int.MaxValue;
    if(argument!=offKey) {
        initialArgs="Checking";
        var reactors = new List<IMyReactor>();
        GridTerminalSystem.GetBlocksOfType<IMyReactor>(reactors);
        float reactorCurrent = 0;
        double uraniumTotal = 0;
        if(reactors.Count>0) {
            foreach(IMyReactor reactor in reactors) {
                if(reactor.IsFunctional==true) {
                    reactorCurrent+=reactor.CurrentOutput;
                    var reactorItems = reactor.GetInventory(0).GetItems();
                    for(int j = reactorItems.Count-1; j>=0; j--) {
                        uraniumTotal+=(double)reactorItems[j].Amount;
                    }
                }
            }
        }
        powerTimeSec = 1/reactorCurrent*60*60*uraniumTotal;
        Echo($"{powerTimeSec.ToString("0.00")} / {(2*60*60)}");
    }
    if(argument==offKey||powerTimeSec<=timeToShutOff) {
        turnedOff=true;
        Echo(argument==offKey ? "Turned off": "EMERGENCY SYSTEM TRIPPED");
        initialArgs=offKey;
        Me.CustomData="";
        foreach(IMyFunctionalBlock block in blocks) {
            if(block is IMyMedicalRoom||
                        block is IMyReactor||
                        block is IMySolarPanel||
                        block is IMyProgrammableBlock||
                        block is IMyTimerBlock||
                        block is IMyButtonPanel) {
                try {
                    block.ApplyAction("OnOff_On");
                } catch {
                    Echo("Failed: "+block.CustomName);
                }
            } else {
                try {
                    block.ApplyAction("OnOff_Off");
                    Me.CustomData+=block.CustomName+"\n";
                } catch {
                    Echo("Failed: "+block.CustomName);
                }
            }
        }
    }
}