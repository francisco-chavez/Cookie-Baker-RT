// <auto-generated>
//  automatically generated by the FlatBuffers compiler, do not modify
// </auto-generated>

namespace FCT.CookieBakerRT.IPC_DataFormat
{

using global::System;
using global::FlatBuffers;

public struct UpAndRunning : IFlatbufferObject
{
  private Table __p;
  public ByteBuffer ByteBuffer { get { return __p.bb; } }
  public static void ValidateVersion() { FlatBufferConstants.FLATBUFFERS_1_11_1(); }
  public static UpAndRunning GetRootAsUpAndRunning(ByteBuffer _bb) { return GetRootAsUpAndRunning(_bb, new UpAndRunning()); }
  public static UpAndRunning GetRootAsUpAndRunning(ByteBuffer _bb, UpAndRunning obj) { return (obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
  public void __init(int _i, ByteBuffer _bb) { __p = new Table(_i, _bb); }
  public UpAndRunning __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }


  public static void StartUpAndRunning(FlatBufferBuilder builder) { builder.StartTable(0); }
  public static Offset<FCT.CookieBakerRT.IPC_DataFormat.UpAndRunning> EndUpAndRunning(FlatBufferBuilder builder) {
    int o = builder.EndTable();
    return new Offset<FCT.CookieBakerRT.IPC_DataFormat.UpAndRunning>(o);
  }
};


}