using RushArena.Core;

int passed = 0;
void Check(string name, Action assertion)
{
    try { assertion(); Console.WriteLine("PASS "+name); passed++; }
    catch(Exception e) { Console.Error.WriteLine("FAIL "+name+": "+e.Message); Environment.Exit(1); }
}
void Assert(bool condition,string message) { if(!condition) throw new Exception(message); }
MovementModel Model() => new(new MovementTuning());
float Travel(int fps)
{
    var m=Model(); float z=0;
    for(int frame=0;frame<fps*2;frame++)
    {
        float remaining=1f/fps;
        while(remaining>0) { float dt=Math.Min(remaining,1f/120f); z+=m.Step(new PlayerCommand{MoveY=1},dt,true,false,0).Z; remaining-=dt; }
    }
    return z;
}
Check("diagonal speed capped",()=>{
    var m=Model(); for(int i=0;i<120;i++) m.Step(new PlayerCommand{MoveX=1,MoveY=1},1f/120f,true,false,0);
    Assert(Math.Abs(m.HorizontalSpeed-7)<0.001f,"diagonal exceeds walk speed");
});
Check("sprint reaches 10.5 m/s",()=>{
    var m=Model(); for(int i=0;i<120;i++) m.Step(new PlayerCommand{MoveY=1,Sprint=true},1f/120f,true,false,0);
    Assert(Math.Abs(m.HorizontalSpeed-10.5f)<0.001f,"sprint target mismatch");
});
Check("ground stop under 120 ms",()=>{
    var m=Model(); for(int i=0;i<120;i++)m.Step(new PlayerCommand{MoveY=1},1f/120f,true,false,0);
    for(int i=0;i<14;i++)m.Step(default,1f/120f,true,false,0);
    Assert(m.HorizontalSpeed<0.001f,"stop response too slow");
});
Check("coyote jump inside 100 ms",()=>{
    var m=Model();m.Step(default,1f/120f,true,false,0);
    for(int i=0;i<6;i++)m.Step(default,1f/120f,false,false,0);
    m.Step(new PlayerCommand{JumpPressed=true},1f/120f,false,false,0);
    Assert(m.JumpedThisStep,"coyote jump missed");
});
Check("coyote window expires",()=>{
    var m=Model();m.Step(default,1f/120f,true,false,0);
    for(int i=0;i<15;i++)m.Step(default,1f/120f,false,false,0);
    m.Step(new PlayerCommand{JumpPressed=true},1f/120f,false,false,0);
    Assert(!m.JumpedThisStep,"late airborne jump accepted");
});
Check("jump buffers across landing",()=>{
    var m=Model();m.Step(new PlayerCommand{JumpPressed=true},1f/120f,false,false,0);
    m.Step(default,1f/120f,true,false,0);Assert(m.JumpedThisStep,"buffer lost before landing");
});
Check("jump buffer expires",()=>{
    var m=Model();m.Step(new PlayerCommand{JumpPressed=true},1f/120f,false,false,0);
    for(int i=0;i<20;i++)m.Step(default,1f/120f,false,false,0);
    m.Step(default,1f/120f,true,false,0);Assert(!m.JumpedThisStep,"stale buffered jump fired");
});
Check("no double jump",()=>{
    var m=Model();m.Step(default,1f/120f,true,false,0);
    m.Step(new PlayerCommand{JumpPressed=true},1f/120f,true,false,0);float v=m.VerticalSpeed;
    m.Step(new PlayerCommand{JumpPressed=true},1f/120f,false,false,0);
    Assert(!m.JumpedThisStep && m.VerticalSpeed<v,"airborne jump repeated");
});
Check("ceiling stops upward velocity",()=>{
    var m=Model();m.Step(default,1f/120f,true,false,0);
    m.Step(new PlayerCommand{JumpPressed=true},1f/120f,true,false,0);
    m.Step(default,1f/120f,false,true,0);Assert(m.VerticalSpeed<=0,"ceiling ignored");
});
Check("terminal fall speed capped",()=>{
    var m=Model();for(int i=0;i<1200;i++)m.Step(default,1f/120f,false,false,0);
    Assert(m.VerticalSpeed==-45,"terminal velocity mismatch");
});
Check("yaw rotates travel direction",()=>{
    var m=Model();var d=m.Step(new PlayerCommand{MoveY=1},1f/120f,true,false,90);
    Assert(d.X>0 && Math.Abs(d.Z)<0.00001,"camera-relative movement mismatch");
});
Check("look pitch clamp and yaw wrap",()=>{
    Assert(LookModel.Pitch(0,1000)==-85 && LookModel.Pitch(0,-1000)==85,"pitch unbounded");
    Assert(LookModel.Yaw(350,20)==10 && LookModel.Yaw(10,-20)==350,"yaw wrap incorrect");
});
Check("30/60/144 FPS travel agrees",()=>{
    float low=Travel(30),mid=Travel(60),high=Travel(144);
    Assert(Math.Abs(low-mid)<0.01 && Math.Abs(low-high)<0.03,"frame-rate-sensitive travel");
});
Check("focus reset clears jump buffer and velocity",()=>{
    var m=Model();m.Step(new PlayerCommand{MoveY=1,JumpPressed=true},1f/120f,false,false,0);m.Reset();
    m.Step(default,1f/120f,true,false,0);Assert(!m.JumpedThisStep && m.HorizontalSpeed==0,"reset retains input");
});
Check("invalid delta requires motor substeps",()=>{
    try {Model().Step(default,0.1f,true,false,0);throw new Exception("oversized delta accepted");}
    catch(ArgumentOutOfRangeException){}
});
Console.WriteLine($"{passed}/15 pure movement checks passed. This is NOT Unity compilation, collision testing or device testing.");
