// <auto-generated>
//  automatically generated by the FlatBuffers compiler, do not modify
// </auto-generated>

namespace FCT.CookieBakerRT.IPC_DataFormat
{

using global::System;
using global::FlatBuffers;

public struct ShutdownMessage : IFlatbufferObject
{
  private Table __p;
  public ByteBuffer ByteBuffer { get { return __p.bb; } }
  public static void ValidateVersion() { FlatBufferConstants.FLATBUFFERS_1_11_1(); }
  public static ShutdownMessage GetRootAsShutdownMessage(ByteBuffer _bb) { return GetRootAsShutdownMessage(_bb, new ShutdownMessage()); }
  public static ShutdownMessage GetRootAsShutdownMessage(ByteBuffer _bb, ShutdownMessage obj) { return (obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
  public void __init(int _i, ByteBuffer _bb) { __p = new Table(_i, _bb); }
  public ShutdownMessage __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }


  public static void StartShutdownMessage(FlatBufferBuilder builder) { builder.StartTable(0); }
  public static Offset<FCT.CookieBakerRT.IPC_DataFormat.ShutdownMessage> EndShutdownMessage(FlatBufferBuilder builder) {
    int o = builder.EndTable();
    return new Offset<FCT.CookieBakerRT.IPC_DataFormat.ShutdownMessage>(o);
  }
};


}
