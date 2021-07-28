void Main()
{ 
	    for ( int i = GridTerminalSystem.Blocks.Count - 1; i >= 0; i-- ){ 
        IMyTerminalBlock test=(GridTerminalSystem.Blocks[i]);
	        	if (test.IsFunctional==false){ 
            test.RequestShowOnHUD(true);
	        	}else{
            test.RequestShowOnHUD(false);
        }
	    } 
}