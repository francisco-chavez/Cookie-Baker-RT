// <auto-generated>
//  automatically generated by the FlatBuffers compiler, do not modify
// </auto-generated>

namespace FCT.CookieBakerRT.IPC_DataFormat
{

using global::System;
using global::FlatBuffers;

public struct ProgressUpdate : IFlatbufferObject
{
  private Table __p;
  public ByteBuffer ByteBuffer { get { return __p.bb; } }
  public static void ValidateVersion() { FlatBufferConstants.FLATBUFFERS_1_11_1(); }
  public static ProgressUpdate GetRootAsProgressUpdate(ByteBuffer _bb) { return GetRootAsProgressUpdate(_bb, new ProgressUpdate()); }
  public static ProgressUpdate GetRootAsProgressUpdate(ByteBuffer _bb, ProgressUpdate obj) { return (obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
  public void __init(int _i, ByteBuffer _bb) { __p = new Table(_i, _bb); }
  public ProgressUpdate __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }

  public int WorkloadID { get { int o = __p.__offset(4); return o != 0 ? __p.bb.GetInt(o + __p.bb_pos) : (int)0; } }
  public int CompletedSamples { get { int o = __p.__offset(6); return o != 0 ? __p.bb.GetInt(o + __p.bb_pos) : (int)0; } }
  public int TotalSamples { get { int o = __p.__offset(8); return o != 0 ? __p.bb.GetInt(o + __p.bb_pos) : (int)0; } }
  public bool CurrentlyRunning { get { int o = __p.__offset(10); return o != 0 ? 0!=__p.bb.Get(o + __p.bb_pos) : (bool)false; } }

  public static Offset<FCT.CookieBakerRT.IPC_DataFormat.ProgressUpdate> CreateProgressUpdate(FlatBufferBuilder builder,
      int WorkloadID = 0,
      int CompletedSamples = 0,
      int TotalSamples = 0,
      bool CurrentlyRunning = false) {
    builder.StartTable(4);
    ProgressUpdate.AddTotalSamples(builder, TotalSamples);
    ProgressUpdate.AddCompletedSamples(builder, CompletedSamples);
    ProgressUpdate.AddWorkloadID(builder, WorkloadID);
    ProgressUpdate.AddCurrentlyRunning(builder, CurrentlyRunning);
    return ProgressUpdate.EndProgressUpdate(builder);
  }

  public static void StartProgressUpdate(FlatBufferBuilder builder) { builder.StartTable(4); }
  public static void AddWorkloadID(FlatBufferBuilder builder, int WorkloadID) { builder.AddInt(0, WorkloadID, 0); }
  public static void AddCompletedSamples(FlatBufferBuilder builder, int CompletedSamples) { builder.AddInt(1, CompletedSamples, 0); }
  public static void AddTotalSamples(FlatBufferBuilder builder, int TotalSamples) { builder.AddInt(2, TotalSamples, 0); }
  public static void AddCurrentlyRunning(FlatBufferBuilder builder, bool CurrentlyRunning) { builder.AddBool(3, CurrentlyRunning, false); }
  public static Offset<FCT.CookieBakerRT.IPC_DataFormat.ProgressUpdate> EndProgressUpdate(FlatBufferBuilder builder) {
    int o = builder.EndTable();
    return new Offset<FCT.CookieBakerRT.IPC_DataFormat.ProgressUpdate>(o);
  }
};


}
