// <auto-generated>
//  automatically generated by the FlatBuffers compiler, do not modify
// </auto-generated>

namespace FCT.CookieBakerRT.IPC_DataFormat
{

using global::System;
using global::FlatBuffers;


import javax.annotation.Nullable;
public struct WorkloadComplete : IFlatbufferObject
{
  private Table __p;
  public ByteBuffer ByteBuffer { get { return __p.bb; } }
  public static void ValidateVersion() { FlatBufferConstants.FLATBUFFERS_1_11_1(); }
  public static WorkloadComplete GetRootAsWorkloadComplete(ByteBuffer _bb) { return GetRootAsWorkloadComplete(_bb, new WorkloadComplete()); }
  public static WorkloadComplete GetRootAsWorkloadComplete(ByteBuffer _bb, WorkloadComplete obj) { return (obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
  public void __init(int _i, ByteBuffer _bb) { __p = new Table(_i, _bb); }
  public WorkloadComplete __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }

  public int WorkloadID { get { int o = __p.__offset(4); return o != 0 ? __p.bb.GetInt(o + __p.bb_pos) : (int)0; } }

  public static Offset<FCT.CookieBakerRT.IPC_DataFormat.WorkloadComplete> CreateWorkloadComplete(FlatBufferBuilder builder,
      int WorkloadID = 0) {
    builder.StartTable(1);
    WorkloadComplete.AddWorkloadID(builder, WorkloadID);
    return WorkloadComplete.EndWorkloadComplete(builder);
  }

  public static void StartWorkloadComplete(FlatBufferBuilder builder) { builder.StartTable(1); }
  public static void AddWorkloadID(FlatBufferBuilder builder, int WorkloadID) { builder.AddInt(0, WorkloadID, 0); }
  public static Offset<FCT.CookieBakerRT.IPC_DataFormat.WorkloadComplete> EndWorkloadComplete(FlatBufferBuilder builder) {
    int o = builder.EndTable();
    return new Offset<FCT.CookieBakerRT.IPC_DataFormat.WorkloadComplete>(o);
  }
};


}
