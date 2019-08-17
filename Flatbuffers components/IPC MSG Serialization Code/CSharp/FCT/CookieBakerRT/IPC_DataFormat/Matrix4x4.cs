// <auto-generated>
//  automatically generated by the FlatBuffers compiler, do not modify
// </auto-generated>

namespace FCT.CookieBakerRT.IPC_DataFormat
{

using global::System;
using global::FlatBuffers;

public struct Matrix4x4 : IFlatbufferObject
{
  private Struct __p;
  public ByteBuffer ByteBuffer { get { return __p.bb; } }
  public void __init(int _i, ByteBuffer _bb) { __p = new Struct(_i, _bb); }
  public Matrix4x4 __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }

  public float M11 { get { return __p.bb.GetFloat(__p.bb_pos + 0); } }
  public float M12 { get { return __p.bb.GetFloat(__p.bb_pos + 4); } }
  public float M13 { get { return __p.bb.GetFloat(__p.bb_pos + 8); } }
  public float M14 { get { return __p.bb.GetFloat(__p.bb_pos + 12); } }
  public float M21 { get { return __p.bb.GetFloat(__p.bb_pos + 16); } }
  public float M22 { get { return __p.bb.GetFloat(__p.bb_pos + 20); } }
  public float M23 { get { return __p.bb.GetFloat(__p.bb_pos + 24); } }
  public float M24 { get { return __p.bb.GetFloat(__p.bb_pos + 28); } }
  public float M31 { get { return __p.bb.GetFloat(__p.bb_pos + 32); } }
  public float M32 { get { return __p.bb.GetFloat(__p.bb_pos + 36); } }
  public float M33 { get { return __p.bb.GetFloat(__p.bb_pos + 40); } }
  public float M34 { get { return __p.bb.GetFloat(__p.bb_pos + 44); } }
  public float M41 { get { return __p.bb.GetFloat(__p.bb_pos + 48); } }
  public float M42 { get { return __p.bb.GetFloat(__p.bb_pos + 52); } }
  public float M43 { get { return __p.bb.GetFloat(__p.bb_pos + 56); } }
  public float M44 { get { return __p.bb.GetFloat(__p.bb_pos + 60); } }

  public static Offset<FCT.CookieBakerRT.IPC_DataFormat.Matrix4x4> CreateMatrix4x4(FlatBufferBuilder builder, float M11, float M12, float M13, float M14, float M21, float M22, float M23, float M24, float M31, float M32, float M33, float M34, float M41, float M42, float M43, float M44) {
    builder.Prep(4, 64);
    builder.PutFloat(M44);
    builder.PutFloat(M43);
    builder.PutFloat(M42);
    builder.PutFloat(M41);
    builder.PutFloat(M34);
    builder.PutFloat(M33);
    builder.PutFloat(M32);
    builder.PutFloat(M31);
    builder.PutFloat(M24);
    builder.PutFloat(M23);
    builder.PutFloat(M22);
    builder.PutFloat(M21);
    builder.PutFloat(M14);
    builder.PutFloat(M13);
    builder.PutFloat(M12);
    builder.PutFloat(M11);
    return new Offset<FCT.CookieBakerRT.IPC_DataFormat.Matrix4x4>(builder.Offset);
  }
};


}