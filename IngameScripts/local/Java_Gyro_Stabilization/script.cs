private static bool running = false;
public void Main(string argument, UpdateType updateSource) {
    if(argument=="Run") {
        running=true;
    }
    if(running) {
        Echo("Running");
        Runtime.UpdateFrequency=UpdateFrequency.Update1;
        running=true;
    } else {
        Echo("Not Running");
        Runtime.UpdateFrequency=UpdateFrequency.Once;
        return;
    }
    IMyShipController control;
    IMyGyro gyro;
    List<IMyShipController> controllers = new List<IMyShipController>();
    GridTerminalSystem.GetBlocksOfType<IMyShipController>(controllers);
    List<IMyGyro> gyroscopes = new List<IMyGyro>();
    GridTerminalSystem.GetBlocksOfType<IMyGyro>(gyroscopes);
    try {
        if(controllers.Count==0||gyroscopes.Count==0) {
            throw new NullReferenceException();
        }
        control=controllers[0];
        gyro=gyroscopes[0];
    } catch(NullReferenceException) {
        Echo($"Required hardware not found");
        return;
    }
    if(argument=="Stop") {
        running=false;
        gyro.GyroOverride=false;
        gyro.GyroPower=1.0f;
        gyro.Pitch=0;
        gyro.Yaw=0;
        gyro.Roll=0;
        return;
    }
    Matrix orientation;
    control.Orientation.GetMatrix(out orientation);
    Vector3D controlDown = orientation.Down;
    Vector3D controlGrav = Vector3D.Normalize(control.GetNaturalGravity());
    gyro.Orientation.GetMatrix(out orientation);
    Vector3D vector1 = Vector3D.Transform(controlDown, MatrixD.Transpose(orientation));
    Vector3D vector2 = Vector3D.Transform(controlGrav, MatrixD.Transpose(gyro.WorldMatrix.GetOrientation()));
    Vector3D diff = Vector3D.Cross(vector1, vector2);
    double diffAng = Math.Atan2(diff.Length(), Math.Sqrt(Math.Max(0.0, 1.0-diff.Length()*diff.Length())));
    Echo($"Angular difference: {diffAng.ToString("0.0000")}");
    diff.Normalize();
    diff*=diffAng*3;
    gyro.Pitch=-(float)diff.GetDim(0);
    gyro.Yaw=-(float)diff.GetDim(1);
    gyro.Roll=-(float)diff.GetDim(2);
    gyro.GyroPower=1.0f;
    gyro.GyroOverride=true;
}