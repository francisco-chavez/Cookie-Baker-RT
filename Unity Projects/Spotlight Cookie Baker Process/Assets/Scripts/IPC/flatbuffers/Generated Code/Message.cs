// <auto-generated>
//  automatically generated by the FlatBuffers compiler, do not modify
// </auto-generated>

namespace FCT.CookieBakerRT.IPC_DataFormat
{

using global::System;
using global::FlatBuffers;

public struct Message : IFlatbufferObject
{
  private Table __p;
  public ByteBuffer ByteBuffer { get { return __p.bb; } }
  public static void ValidateVersion() { FlatBufferConstants.FLATBUFFERS_1_11_1(); }
  public static Message GetRootAsMessage(ByteBuffer _bb) { return GetRootAsMessage(_bb, new Message()); }
  public static Message GetRootAsMessage(ByteBuffer _bb, Message obj) { return (obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
  public void __init(int _i, ByteBuffer _bb) { __p = new Table(_i, _bb); }
  public Message __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }

  public FCT.CookieBakerRT.IPC_DataFormat.MessageDatum DataType { get { int o = __p.__offset(4); return o != 0 ? (FCT.CookieBakerRT.IPC_DataFormat.MessageDatum)__p.bb.Get(o + __p.bb_pos) : FCT.CookieBakerRT.IPC_DataFormat.MessageDatum.NONE; } }
  public TTable? Data<TTable>() where TTable : struct, IFlatbufferObject { int o = __p.__offset(6); return o != 0 ? (TTable?)__p.__union<TTable>(o) : null; }

  public static Offset<FCT.CookieBakerRT.IPC_DataFormat.Message> CreateMessage(FlatBufferBuilder builder,
      FCT.CookieBakerRT.IPC_DataFormat.MessageDatum Data_type = FCT.CookieBakerRT.IPC_DataFormat.MessageDatum.NONE,
      int DataOffset = 0) {
    builder.StartTable(2);
    Message.AddData(builder, DataOffset);
    Message.AddDataType(builder, Data_type);
    return Message.EndMessage(builder);
  }

  public static void StartMessage(FlatBufferBuilder builder) { builder.StartTable(2); }
  public static void AddDataType(FlatBufferBuilder builder, FCT.CookieBakerRT.IPC_DataFormat.MessageDatum DataType) { builder.AddByte(0, (byte)DataType, 0); }
  public static void AddData(FlatBufferBuilder builder, int DataOffset) { builder.AddOffset(1, DataOffset, 0); }
  public static Offset<FCT.CookieBakerRT.IPC_DataFormat.Message> EndMessage(FlatBufferBuilder builder) {
    int o = builder.EndTable();
    return new Offset<FCT.CookieBakerRT.IPC_DataFormat.Message>(o);
  }
  public static void FinishMessageBuffer(FlatBufferBuilder builder, Offset<FCT.CookieBakerRT.IPC_DataFormat.Message> offset) { builder.Finish(offset.Value); }
  public static void FinishSizePrefixedMessageBuffer(FlatBufferBuilder builder, Offset<FCT.CookieBakerRT.IPC_DataFormat.Message> offset) { builder.FinishSizePrefixed(offset.Value); }
};


}
