using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

public class Conv
{
    static public string SH2SUntilNull_3E(string hexa_data_string, bool reverted = false, int add_byte = 0)
    {
        string sBuff = hexa_data_string + " ";
        string sBuff2 = "";
        int i = 0;
        sBuff = sBuff.Replace(" ", "");
        int iLen = sBuff.Length / 2;
        if (reverted)
        {
            for (i = iLen - 1; i >= 0; i += -1)
            {
                sBuff2 += Strings.Chr(Convert.ToInt32(Conversion.Val("&H" + sBuff.Substring(i * 2, 2))));
            }
        }
        else
        {
            for (i = 0; i <= iLen - 1; i++)
            {
                sBuff2 += Strings.Chr(Convert.ToInt32(Conversion.Val("&H" + sBuff.Substring(i * 2, 2))));
            }
        }


        if (sBuff2.Contains("\0")) sBuff2 = sBuff2.Substring(0, sBuff2.IndexOf("\0")).Trim();
        //string strNew = "";
        //int idxlengt = sBuff2.Length % 2 == 0 ? sBuff2.Length / 2 : sBuff2.Length / 2 + 1;
        //for (int idx = 0; idx < idxlengt; idx++)
        //{
        //    if (sBuff2.Length > idx * 2 + 1)
        //        strNew += sBuff2[idx * 2 + 1];
        //    if (sBuff2.Length > idx * 2)
        //        strNew += sBuff2[idx * 2];
        //}
        return sBuff2;
    }
    static public string SH2SUntilNull(string hexa_data_string, bool reverted = false, int add_byte = 0)
    {
        string sBuff = hexa_data_string + " ";
        string sBuff2 = "";
        int i = 0;
        sBuff = sBuff.Replace(" ", "");
        int iLen = sBuff.Length / 2;
        if (reverted)
        {
            for (i = iLen - 1; i >= 0; i += -1)
            {
                sBuff2 += Strings.Chr(Convert.ToInt32(Conversion.Val("&H" + sBuff.Substring(i * 2, 2))));
            }
        }
        else
        {
            for (i = 0; i <= iLen - 1; i++)
            {
                sBuff2 += Strings.Chr(Convert.ToInt32(Conversion.Val("&H" + sBuff.Substring(i * 2, 2))));
            }
        }


        if (sBuff2.Contains("\0")) sBuff2 = sBuff2.Substring(0, sBuff2.IndexOf("\0")).Trim();
        string strNew = "";
        int idxlengt = sBuff2.Length % 2 == 0 ? sBuff2.Length / 2 : sBuff2.Length / 2 + 1;
        for (int idx = 0; idx < idxlengt; idx++)
        {
            if (sBuff2.Length > idx * 2 + 1)
                strNew += sBuff2[idx * 2 + 1];
            if (sBuff2.Length > idx * 2)
                strNew += sBuff2[idx * 2];
        }
        return strNew;
    }
    static public short SHtoi16(object obj, short default_value = 0, int from_base = 10)
    {
        short ret = default_value;
        if (obj != null && !object.ReferenceEquals(obj, DBNull.Value))
        {
            try
            {
                ret = Convert.ToInt16(obj.ToString(), from_base);
                //string rs = Convert.ToString(Convert.ToInt32(obj), from_base);
                //ret = Convert.ToInt32(rs);
            }
            catch (Exception ex)
            {
            }
        }
        return ret;
    }
    static public int SHtoi32(object obj, short default_value = 0, int from_base = 10)
    {
        int ret = default_value;
        if (obj != null && !object.ReferenceEquals(obj, DBNull.Value))
        {
            try
            {
                ret = Convert.ToInt32(obj.ToString(), from_base);
                //string rs = Convert.ToString(Convert.ToInt32(obj), from_base);
                //ret = Convert.ToInt32(rs);
            }
            catch (Exception ex)
            {
            }
        }
        return ret;
    }

    public static int[] HexStringToIntArray2(string hexString)
    {
        int[] intArray = new int[0];
        if (hexString.Length % 4 == 0)
        {
            try
            {
                intArray = new int[hexString.Length / 4];
                for (int i = 0; i < intArray.Length; i++)
                {
                    string val = hexString.Substring(i * 4, 4);
                    string swap = val.Substring(2) + val.Substring(0, 2);
                    intArray[i] = int.Parse(swap, System.Globalization.NumberStyles.HexNumber);
                    
                }
            }
            catch (Exception)
            {
                return new int[0];
            }
        }
        else
        {
            hexString = hexString.PadRight((hexString.Length / 4 + 1) * 4, '0');

            try
            {
                intArray = new int[hexString.Length / 4];
                for (int i = 0; i < intArray.Length; i++)
                {
                    string val = hexString.Substring(i * 4, 4);
                    string swap = val.Substring(2) + val.Substring(0, 2);
                    intArray[i] = int.Parse(swap, System.Globalization.NumberStyles.HexNumber);
                }
            }
            catch (Exception)
            {
                return new int[0];
            }
        }

        return intArray;
    }
    static public int SHtoi32(object obj, int default_value = 0, int from_base = 10)
    {
        int ret = default_value;
        if (obj != null && !object.ReferenceEquals(obj, DBNull.Value))
        {
            try
            {
                ret = Convert.ToInt32(obj.ToString(), from_base);
                //string rs = Convert.ToString(Convert.ToInt32(obj), from_base);
                //ret = Convert.ToInt32(rs);
            }
            catch (Exception ex)
            {
            }
        }
        return ret;
    }

    public static int HexStringToInt(string hexString)
    {
        if (hexString.Length % 2 == 0)
        {
            try
            {
                int intValue = int.Parse(hexString, System.Globalization.NumberStyles.HexNumber);
                return intValue;
            }
            catch (Exception)
            {

            }
        }
        return int.MinValue;
    }
    static public string ByteArrayToHexString(byte[] ba)
    {
        try
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }
        catch (Exception)
        {
        }
        return "";
    }
    public static byte[] IntArr2ByteArr(int[] ints)
    {
        try
        {
            Int16[] ints16 = new Int16[ints.Length * 2];
            for(int i = 0; i< ints.Length; i++)
            {
                ints16[i * 2] = (Int16)(ints[i] % 65536);
                ints16[i * 2 + 1] = (Int16)(ints[i] / 65536);
            }
            byte[] bytes = ints16.SelectMany(BitConverter.GetBytes).ToArray();
            byte[] bytesInvert = new byte[bytes.Length];
            for (int i = 0; i < bytesInvert.Length/2; i++)
            {
                bytesInvert[i * 2] = bytes[i * 2 + 1];
                bytesInvert[i * 2 + 1] = bytes[i * 2];
            }
            return bytesInvert;
        }
        catch
        {

        }
        return new byte[0];
    }
    public static byte[] Int2ByteArr(int intsValue)
    {
        try
        {
            Int16[] ints16 = new Int16[1];
            ints16[0] = (Int16)(intsValue % 65536);
            byte[] bytes = ints16.SelectMany(BitConverter.GetBytes).ToArray();
            byte[] bytesInvert = new byte[bytes.Length];
            for (int i = 0; i < bytesInvert.Length / 2; i++)
            {
                bytesInvert[i * 2] = bytes[i * 2 + 1];
                bytesInvert[i * 2 + 1] = bytes[i * 2];
            }
            return bytesInvert;
        }
        catch
        {

        }
        return new byte[0];
    }

    public static int[] Int32toInt16(Int32 value)
    {
        int[] rst = new int[2];
        rst[0] = (int)(value % 65535);
        rst[1] = (int)(value / 65535);
        return rst;
    }
    public static int[] Int32toInt16(int numRegister, Int32[] values)
    {
        int[] rst = new int[numRegister * 2];
        for(int i = 0; i< numRegister; i++)
        {
            rst[i * 2] = values.Length > i ? (int)(values[i] % 65535) : 0;
            rst[i * 2 + 1] = values.Length > i ? (int)(values[i] / 65535) : 0;
        }
        return rst;
    }
    public static byte[] HexToBytes(string hex)
    {
        if (hex == null)
            throw new ArgumentNullException("The data is null");

        if (hex.Length % 2 != 0)
            throw new FormatException("Hex Character Count Not Even");

        byte[] bytes = new byte[hex.Length / 2];

        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);

        return bytes;
    }

    public static string BytesToStringHex(byte[] bytes)
    {
        return BitConverter.ToString(bytes).Replace("-", "");
    }

    public static int ToInt16(byte hight_byte, byte low_byte)
    {
        int retValue = (hight_byte << 8) | low_byte;
        if (retValue > 0xefff) retValue -= 0x10000;
        return retValue;
    }

    public static int ToUInt16(byte hight_byte, byte low_byte)
    {
        int retValue = (hight_byte << 8) | low_byte;
        return retValue;
    }

    public static bool[] ToBoolArray(int i)
    {
        return Convert.ToString(i, 2 /*for binary*/).Select(s => s.Equals('1')).ToArray();
    }

    public static bool[] Uint16ToBoolArray(byte hight_byte, byte low_byte)
    {
        int i = ToUInt16(hight_byte, low_byte);
        bool[] res = Convert.ToString(i, 2 /*for binary*/).PadLeft(16, '0').Select(s => s.Equals('1')).ToArray();
        Array.Reverse(res);
        return res;
    }

    public static string Br2S(byte[] bytes)
    {
        string str = "";
        if (bytes != null)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                str += (char)bytes[i];
            }
        }
        return str;
    }

    public static byte[] S2Br(String str)
    {
        char[] chars = str.ToCharArray();
        byte[] bytes = new byte[chars.Length];
        for (int i = 0; i < chars.Length; i++) bytes[i] = (byte)chars[i];
        return bytes;
    }

    public static bool atob(object obj)
    {
        bool ret = false;
        try { ret = Convert.ToBoolean(obj); }
        catch (Exception ex) { }
        return ret;
    }

    public static string atos(object obj)
    {
        string ret = "";
        if (obj != null && obj != DBNull.Value) ret = obj.ToString();
        return ret;
    }

    public static double atod(object obj, int default_value)
    {
        double retValue = default_value;
        try
        {
            retValue = Convert.ToDouble(obj);
        }
        catch
        {
        }
        return retValue;
    }
    public static DateTime atodt(object obj)
    {
        DateTime retValue = new DateTime(2000, 1, 1);
        try
        {
            retValue = Convert.ToDateTime(obj);
        }
        catch
        {
        }
        return retValue;
    }

    public static double atod(object obj) { return atod(obj, 0); }

    public static int atoi32(object obj, int min_value, int max_value)
    {
        int retValue = min_value;
        try
        {
            retValue = Convert.ToInt32(obj);
        }
        catch
        {
        }
        if (max_value < int.MaxValue) //Co su dung gia tri max
        {
            if (retValue < min_value) retValue = min_value;
            if (retValue > max_value) retValue = max_value;
        }
        return retValue;
    }

    public static int atoi32(object obj, int default_value) { return atoi32(obj, default_value, int.MaxValue); }
    public static int atoi32(object obj) { return atoi32(obj, 0, int.MaxValue); }

    public static uint atoui32(object obj)
    {
        uint retValue = 0;
        try
        {
            retValue = Convert.ToUInt32(obj);
        }
        catch
        {
        }
        return retValue;
    }







    /* bin to 8421BCD */
    public static int BIN2BCD8(int bin) { return (bin % 10) + ((bin / 10) % 10) * 0x10; }
    public static int BIN2BCD16(int bin)
    {
        return (bin % 10) + ((bin / 10) % 10) * 0x10 + ((bin / 100) % 10) * 0x100 + ((bin / 1000) % 10) * 0x1000;
    }
    public static UInt32 BIN2BCD24(UInt32 bin)
    {
        return (bin % 10) + ((bin / 10) % 10) * 0x10 + ((bin / 100) % 10) * 0x100 + ((bin / 1000) % 10) * 0x1000 + ((bin / 10000) % 10) * 0x10000 + ((bin / 100000) % 10) * 0x100000;
    }
    public static UInt32 BIN2BCD32(UInt32 bin)
    {
        return ((UInt32)(BIN2BCD16((int)((bin >> 16) & 0xFFFF))) << 16) + (UInt32)BIN2BCD16((int)(bin & 0xFFFF));
    }
    public static UInt64 BIN2BCD64(UInt64 bin)
    {
        return ((UInt64)(BIN2BCD32((UInt32)((bin >> 32) & 0xFFFFFFFF))) << 32) + BIN2BCD32((UInt32)(bin & 0xFFFFFFFF));
    }
    /* 8421BCD to bin*/
    public static int BCD2BIN8(int bcd) { return (bcd & 0xF) + ((bcd >> 4) & 0xF) * 10; }
    public static int BCD2BIN16(int bcd)
    {
        return (bcd & 0xF) + ((bcd >> 4) & 0xF) * 10 + ((bcd >> 8) & 0xF) * 100 + ((bcd >> 12) & 0xF) * 1000;
    }
    public static UInt32 BCD2BIN24(UInt32 bcd)
    {
        return (bcd & 0xF) + ((bcd >> 4) & 0xF) * 10 + ((bcd >> 8) & 0xF) * 100 + ((bcd >> 12) & 0xF) * 1000 + ((bcd >> 16) & 0xF) * 10000 + ((bcd >> 20) & 0xF) * 100000;
    }
    public static UInt32 BCD2BIN32(UInt32 bcd)
    {
        return (UInt32)(BCD2BIN16((int)((bcd >> 16) & 0xFFFF)) << 16) + (UInt32)BCD2BIN16((int)(bcd & 0xFFFF));
    }
    public static UInt64 BCD2BIN64(UInt64 bcd)
    {
        return (UInt64)((UInt32)(BCD2BIN32((UInt32)((bcd >> 32) & 0xFFFFFFFF))) << 32) + (UInt64)BCD2BIN32((UInt32)(bcd & 0xFFFFFFFF));
    }

    public static string bytesToString(byte[] bytes)
    {
        string str = "";
        if (bytes != null)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                str += (char)bytes[i];
            }
        }
        return str;
    }

    public static byte[] stringToBytes(String str)
    {
        char[] chars = str.ToCharArray();
        byte[] bytes = new byte[chars.Length];
        for (int i = 0; i < chars.Length; i++) bytes[i] = (byte)chars[i];
        return bytes;
    }

    public static string U2SH48(long value) { return S2SH(U2S16(value >> 32), 0, "") + S2SH(U2S32(value), 0, ""); }

    public static string U2SH32(long value) { return S2SH(U2S32(value), 0, ""); }
    public static string U2SH32(long value3, long value2, long value1, long value0) { return S2SH(U2S32(value3, value2, value1, value0), 0, ""); }

    public static string U2S32(long value) { return U2S32(value >> 24, value >> 16, value >> 8, value); }
    public static string U2S32(long value3, long value2, long value1, long value0)
    {
        string retValue = "";
        retValue += (char)(value3 & 0xFF);
        retValue += (char)(value2 & 0xFF);
        retValue += (char)(value1 & 0xFF);
        retValue += (char)(value0 & 0xFF);
        return retValue;
    }

    public static UInt32 S2U16(string value, bool is_swap, int add_byte)
    {
        UInt32 retValue = 0;
        byte[] bytes = stringToBytes(value);
        if (is_swap)
        {
            retValue = (UInt32)(bytes[1] + add_byte);
            retValue = (retValue << 8) + (UInt32)(bytes[0] + add_byte);
        }
        else
        {
            retValue = (UInt32)(bytes[0] + add_byte);
            retValue = (retValue << 8) + (UInt32)(bytes[1] + add_byte);
        }
        return retValue;
    }

    public static UInt32 S2U24(string value, bool is_swap, int add_byte)
    {
        UInt32 retValue = 0;
        byte[] bytes = stringToBytes(value);
        if (is_swap)
        {
            retValue = (UInt32)(bytes[2] + add_byte);
            retValue = (retValue << 8) + (UInt32)(bytes[1] + add_byte);
            retValue = (retValue << 8) + (UInt32)(bytes[0] + add_byte);
        }
        else
        {
            retValue = (UInt32)(bytes[0] + add_byte);
            retValue = (retValue << 8) + (UInt32)(bytes[1] + add_byte);
            retValue = (retValue << 8) + (UInt32)(bytes[2] + add_byte);
        }
        return retValue;
    }

    public static UInt32 S2U32(string value, bool is_swap, int add_byte)
    {
        UInt32 retValue = 0;
        byte[] bytes = stringToBytes(value);
        if (is_swap)
        {
            retValue = (UInt32)(bytes[3] + add_byte);
            retValue = (retValue << 8) + (UInt32)(bytes[2] + add_byte);
            retValue = (retValue << 8) + (UInt32)(bytes[1] + add_byte);
            retValue = (retValue << 8) + (UInt32)(bytes[0] + add_byte);
        }
        else
        {
            retValue = (UInt32)(bytes[0] + add_byte);
            retValue = (retValue << 8) + (UInt32)(bytes[1] + add_byte);
            retValue = (retValue << 8) + (UInt32)(bytes[2] + add_byte);
            retValue = (retValue << 8) + (UInt32)(bytes[3] + add_byte);
        }
        return retValue;
    }

    public static UInt64 S2U64(string value, bool is_swap, int add_byte)
    {
        UInt64 retValue = 0;
        byte[] bytes = stringToBytes(value);
        if (is_swap)
        {
            retValue = (UInt64)(bytes[7] + add_byte);
            retValue = (retValue << 8) + (UInt64)(bytes[6] + add_byte);
            retValue = (retValue << 8) + (UInt64)(bytes[5] + add_byte);
            retValue = (retValue << 8) + (UInt64)(bytes[4] + add_byte);
            retValue = (retValue << 8) + (UInt64)(bytes[3] + add_byte);
            retValue = (retValue << 8) + (UInt64)(bytes[2] + add_byte);
            retValue = (retValue << 8) + (UInt64)(bytes[1] + add_byte);
            retValue = (retValue << 8) + (UInt64)(bytes[0] + add_byte);
        }
        else
        {
            retValue = (UInt64)(bytes[0] + add_byte);
            retValue = (retValue << 8) + (UInt64)(bytes[1] + add_byte);
            retValue = (retValue << 8) + (UInt64)(bytes[2] + add_byte);
            retValue = (retValue << 8) + (UInt64)(bytes[3] + add_byte);
            retValue = (retValue << 8) + (UInt64)(bytes[4] + add_byte);
            retValue = (retValue << 8) + (UInt64)(bytes[5] + add_byte);
            retValue = (retValue << 8) + (UInt64)(bytes[6] + add_byte);
            retValue = (retValue << 8) + (UInt64)(bytes[7] + add_byte);
        }
        return retValue;
    }

    public static string U2S64(UInt64 value, bool is_swap, int add_byte)
    {
        byte b7, b6, b5, b4, b3, b2, b1, b0;
        if (is_swap)
        {
            b7 = (byte)((int)(value & 0xFF) + add_byte);
            b6 = (byte)((int)(value >> 8 & 0xFF) + add_byte);
            b5 = (byte)((int)(value >> 16 & 0xFF) + add_byte);
            b4 = (byte)((int)(value >> 24 & 0xFF) + add_byte);
            b3 = (byte)((int)(value >> 32 & 0xFF) + add_byte);
            b2 = (byte)((int)(value >> 40 & 0xFF) + add_byte);
            b1 = (byte)((int)(value >> 48 & 0xFF) + add_byte);
            b0 = (byte)((int)(value >> 56 & 0xFF) + add_byte);
        }
        else
        {
            b0 = (byte)((int)(value & 0xFF) + add_byte);
            b1 = (byte)((int)(value >> 8 & 0xFF) + add_byte);
            b2 = (byte)((int)(value >> 16 & 0xFF) + add_byte);
            b3 = (byte)((int)(value >> 24 & 0xFF) + add_byte);
            b4 = (byte)((int)(value >> 32 & 0xFF) + add_byte);
            b5 = (byte)((int)(value >> 40 & 0xFF) + add_byte);
            b6 = (byte)((int)(value >> 48 & 0xFF) + add_byte);
            b7 = (byte)((int)(value >> 56 & 0xFF) + add_byte);
        }
        return U2S64(b7, b6, b5, b4, b3, b2, b1, b0);
    }
    public static string U2S64(byte value7, byte value6, byte value5, byte value4, byte value3, byte value2, byte value1, byte value0)
    {
        string retValue = "";
        retValue += (char)value7;
        retValue += (char)value6;
        retValue += (char)value5;
        retValue += (char)value4;
        retValue += (char)value3;
        retValue += (char)value2;
        retValue += (char)value1;
        retValue += (char)value0;
        return retValue;
    }

    public static string U2SH16(long value) { return S2SH(U2S16(value), 0, ""); }
    public static string U2SH16(long value1, long value0) { return S2SH(U2S16(value1, value0), 0, ""); }

    public static string U2S16(long value) { return U2S16(value >> 8, value); }
    public static string U2S16(long value1, long value0)
    {
        string retValue = "";
        retValue += (char)(value1 & 0xFF);
        retValue += (char)(value0 & 0xFF);
        return retValue;
    }

    public static long atoi64(object obj) { return atoi64(obj, 10); }
    public static long atoi64(object obj, int fromBase)
    {
        long retValue = 0;
        try
        {
            retValue = Convert.ToInt64(obj.ToString(), fromBase);
        }
        catch { }
        return retValue;
    }

    public static ulong atou64(object obj) { return atou64(obj, 10); }
    public static ulong atou64(object obj, int fromBase)
    {
        ulong retValue = 0;
        try
        {
            retValue = Convert.ToUInt64(obj.ToString(), fromBase);
        }
        catch { }
        return retValue;
    }

    public static string SH2S_New(string sour) 
    { 
        string str =  SH2S(sour, 0);
        if (str.Contains('\0'))
        {
            str = str.Substring(0,str.IndexOf('\0'));
        }
        return str;
    }


    public static string SH2S(string sour, int add_value, bool swap)
    {
        int len;
        string sBuff = "";
        try
        {
            string sdata = sour.Replace(" ", "");
            if (sdata.Length % 2 == 1) sdata = "0" + sdata;
            len = sdata.Length / 2;
            if (len > 0)
            {
                if (swap)
                {
                    for (int i = len - 1; i > -1; i--)
                    {
                        sBuff += Convert.ToChar(Convert.ToInt32(sdata.Substring(i * 2, 2), 16) + add_value);
                    }
                }
                else
                {
                    for (int i = 0; i < len; i++)
                    {
                        sBuff += Convert.ToChar(Convert.ToInt32(sdata.Substring(i * 2, 2), 16) + add_value);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            //Debug.P("[SH2S]-" + ex.Message + "-[" + sour + "]\r\n", Color.Black);
        }
        return sBuff;
    }
    public static string SH2S(string sour, int add_value) { return SH2S(sour, add_value, false); }
    public static string SH2S(string sour) { return SH2S(sour, 0); }

    public static string S2SH(string sour) { return S2SH(sour, false, 0, " "); }
    public static string S2SH(string sour, int add_value, string space) { return S2SH(sour, false, add_value, space); }
    public static string S2SH(string sour, bool is_swap, int add_value, string space)
    {
        string sBuff = "0123456789ABCDEF";
        char[] hexArray = sBuff.ToCharArray();
        int len, i = 0;
        int temp;

        len = sour.Length;
        sBuff = "";
        try
        {
            if (len > 0)
            {
                for (i = 0; i < len; i++)
                {
                    if (is_swap) temp = (sour[len - i - 1] & 0xFF) + add_value;
                    else temp = (sour[i] & 0xFF) + add_value;
                    sBuff += hexArray[temp >> 4];
                    sBuff += hexArray[temp & 0x0F];
                    sBuff += space;
                }
            }
        }
        catch (Exception ex)
        {
            //AddLogWindow("[S2SH]-" + ex.Message + "-[" + sour + "]\r\n", Color.Black);
        }
        return sBuff; //
    }
    /*
    string CRC16_Caculate(string data)
    {
        ushort crc = 0xFFFF; //CRC Init
        ushort temp;
        string sBuff = "";
        byte[] bytes = stringToBytes(data);

        for (int i = 0; i < bytes.Length; i++)
        {
            temp = bytes[i];
            for (int j = 0; j < 8; j++)
            {
                if ((((crc & 0x8000) >> 8) ^ (temp & 0x80)) > 0)
                    crc = (ushort)((crc << 1) ^ 0x8005); //CRC16_POLY 
                else
                    crc = (ushort)(crc << 1);
                temp <<= 1;
            }
        }
        sBuff += (char)((crc >> 8) & 0xFF);
        sBuff += (char)(crc & 0xFF);
        return sBuff;
    }
    */
    //CRC16_Caculate và CalcCRC16 cho ra cung mot ket qua
    public static string CalcCRC16(string data)
    {
        string sBuff = "";
        ushort crc = 0xFFFF;//CRC Init
        byte[] bytes = stringToBytes(data);
        for (int i = 0; i < data.Length; i++)
        {
            crc ^= (ushort)(bytes[i] << 8);
            for (int j = 0; j < 8; j++)
            {
                if ((crc & 0x8000) > 0)
                    crc = (ushort)((crc << 1) ^ 0x8005); //CRC16_POLY 
                else
                    crc <<= 1;
            }
        }
        sBuff += (char)((crc >> 8) & 0xFF);
        sBuff += (char)(crc & 0xFF);
        return sBuff;
    }

}
