using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using DamienG.Security.Cryptography;

namespace Nus3Audio_Editor
{
    class nus3audio
    {
        public int get_padding_amount(int offset)
        {
            return ((0x18 - (offset % 0x10)) % 0x10);
        }

        public byte[] intToBytes(int number)
        {
            return BitConverter.GetBytes((UInt32)number);
        }

        public byte[] stringToBytes(string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }

        public string GetMagic(byte[] b)
        {
            return $"{Convert.ToChar(b[0])}{Convert.ToChar(b[1])}{Convert.ToChar(b[2])}{Convert.ToChar(b[3])}";;
        }

        public struct FileStorage
        {
            public UInt32 fileOffset;
            public UInt32 fileSize;
        }

        public abstract class Entry
        {
            public char[] magic = new char[4];
            public UInt32 size;
            public virtual void Read(BinaryReader f)
            {
                magic = f.ReadChars(4);
                size = f.ReadUInt32();
            }
        }

        public class HEADER : Entry { }

        public class AUDIINDX : Entry
        {
            public new char[] magic = new char[8];
            public UInt32 count;

            public override void Read(BinaryReader f)
            {
                magic = f.ReadChars(8);
                size = f.ReadUInt32();
                count = f.ReadUInt32();
            }
        }

        public class TNID : Entry
        {
            UInt32[] trackNumbers;
            byte[] data;

            public UInt32[] TrackNumbers
            {
                get
                {
                    return trackNumbers;
                }
            }

            public void Read(BinaryReader f, UInt32 AudiindxCount)
            {
                base.Read(f);
                if (size >= AudiindxCount * 4)
                {
                    trackNumbers = new UInt32[AudiindxCount];
                    for (var i = 0; i < AudiindxCount; i++)
                    {
                        trackNumbers[i] = f.ReadUInt32();
                    }
                }
                else
                {
                    data = new byte[size];
                    for (var i = 0; i < size; i++)
                    {
                        data[i] = f.ReadByte();
                    }
                }
            }
        }

        public class NMOF : Entry
        {
            UInt32[] names;
            byte[] data;

            public void Read(BinaryReader f, UInt32 AudiindxCount)
            {
                base.Read(f);
                if (size >= AudiindxCount * 4)
                {
                    names = new UInt32[AudiindxCount];
                    for (var i = 0; i < AudiindxCount; i++)
                    {
                        names[i] = f.ReadUInt32();
                    }
                }
                else
                {
                    data = new byte[size];
                    for (var i = 0; i < size; i++)
                    {
                        data[i] = f.ReadByte();
                    }
                }
            }
        }

        public class ADOF : Entry
        {
            public FileStorage[] fileEntries;
            byte[] data;

            public void Read(BinaryReader f, UInt32 AudiindxCount)
            {
                base.Read(f);
                if (size >= AudiindxCount * 4)
                {
                    fileEntries = new FileStorage[AudiindxCount];
                    for (var i = 0; i < AudiindxCount; i++)
                    {
                        fileEntries[i].fileOffset = f.ReadUInt32();
                        fileEntries[i].fileSize = f.ReadUInt32();
                    }
                }
                else
                {
                    data = new byte[size];
                    for (var i = 0; i < size; i++)
                    {
                        data[i] = f.ReadByte();
                    }
                }
            }
        }

        public class TNNM : Entry
        {
            public string[] string_section;

            public override void Read(BinaryReader f)
            {
                base.Read(f);
                List<string> temp_hold = new List<string>();
                string tone_name = "";
                for (var i = 0; i < size; i++)
                {
                    byte res = f.ReadByte();
                    if (res != 0x00)
                    {
                        tone_name += Convert.ToChar(res).ToString();
                    }
                    else
                    {
                        temp_hold.Add(tone_name);
                        tone_name = "";
                    }
                }
                string_section = temp_hold.ToArray();
            }
        }

        public class JUNK : Entry
        {
            byte[] padding;

            public override void Read(BinaryReader f)
            {
                base.Read(f);
                padding = new byte[size];
                for (var i = 0; i < size; i++)
                {
                    padding[i] = f.ReadByte();
                }
            }
        }

        public class PACK : Entry { }

        public class FileEntry
        {
            public string toneName;
            public byte[] fileData;
        }

        public HEADER head = new HEADER();
        public AUDIINDX audi = new AUDIINDX();
        public TNID tnid = new TNID();
        public NMOF nmof = new NMOF();
        public ADOF adof = new ADOF();
        public TNNM tnnm = new TNNM();
        public JUNK junk = new JUNK();
        public PACK pack = new PACK();

        public List<FileEntry> files = new List<FileEntry>();

        public nus3audio() { }

        public nus3audio(string path)
        {
            BinaryReader file = new BinaryReader(File.Open(path, FileMode.Open));
            head.Read(file);
            audi.Read(file);
            tnid.Read(file, audi.count);
            nmof.Read(file, audi.count);
            adof.Read(file, audi.count);
            tnnm.Read(file);
            junk.Read(file);
            pack.Read(file);

            for (var i = 0; i < adof.fileEntries.Length; i++) {
                file.BaseStream.Seek(adof.fileEntries[i].fileOffset, SeekOrigin.Begin);

                FileEntry entry = new FileEntry();

                entry.toneName = tnnm.string_section[i];
                entry.fileData = file.ReadBytes((int)adof.fileEntries[i].fileSize);

                files.Add(entry);
            }

            file.Close();
        }

        public struct FileOffset
        {
            public UInt32 offset;
            public UInt32 size;
        }

        public bool Write(BinaryWriter writer)
        {
            FileEntry[] files_arr = files.ToArray();

            UInt32[] string_offsets = new UInt32[files_arr.Length];
            FileOffset[] file_offsets = new FileOffset[files_arr.Length];

            int nus3_size = "NUS3".Length + 4;
            int audi_size = "AUDIINDX".Length + (4 * 2);
            int tnid_size = "TNID".Length + 4 + (4 * files_arr.Length);
            int nmof_size = tnid_size;
            int adof_size = "ADOF".Length + 4 + (4 * files_arr.Length * 2);

            int string_section_start = nus3_size + audi_size + tnid_size + nmof_size + adof_size + "TNNM".Length + 4;
            int string_section_size = 0;

            for (int i = 0; i < files_arr.Length; i++)
            {
                string_offsets[i] = (UInt32)(string_section_start + string_section_size);
                string_section_size += files[i].toneName.Length + 1;
            }

            int junk_pad = get_padding_amount(string_section_start + string_section_size + "JUNK".Length + 4);
            int junk_size = "JUNK".Length + 4 + junk_pad;

            int pack_section_start = string_section_start + string_section_size + junk_size + "PACK".Length + 4;

            int pack_section_size_no_pad = 0;
            int pack_section_size = 0;
            Dictionary<UInt32, FileOffset> existing_files = new Dictionary<UInt32, FileOffset>();
            List<FileEntry> files_to_pack = new List<FileEntry>();

            for (var i = 0; i < files_arr.Length; i++)
            {
                UInt32 hash = Crc32.Compute(files[i].fileData);

                FileOffset offset_pair;

                if (existing_files.ContainsKey(hash))
                {
                    offset_pair = existing_files[hash];
                }
                else
                {
                    offset_pair.offset = (UInt32)(pack_section_start + pack_section_size);
                    offset_pair.size = (UInt32)files[i].fileData.Length;

                    existing_files.Add(hash, offset_pair);
                    files_to_pack.Add(files[i]);
                    pack_section_size_no_pad = pack_section_size + files[i].fileData.Length;
                    pack_section_size += (int)((files[i].fileData.Length + 0xF) / 0x10) * 0x10;
                }
                file_offsets[i] = offset_pair;
            }

            if (files_arr.Length == 1)
            {
                pack_section_size = pack_section_size_no_pad;
            }

            int filesize = pack_section_start + pack_section_size;

            writer.Write(stringToBytes("NUS3"));
            writer.Write(intToBytes(filesize - nus3_size));

            writer.Write(stringToBytes("AUDIINDX"));
            writer.Write(intToBytes(4));
            writer.Write(intToBytes(files_arr.Length));

            writer.Write(stringToBytes("TNID"));
            writer.Write(intToBytes(files_arr.Length * 4));
            for (var i = 0; i < files_arr.Length; i++)
                writer.Write(intToBytes(i));

            writer.Write(stringToBytes("NMOF"));
            writer.Write(intToBytes(files_arr.Length * 4));
            foreach (int offset in string_offsets)
                writer.Write(intToBytes(offset));

            writer.Write(stringToBytes("ADOF"));
            writer.Write(intToBytes(files_arr.Length * 8));
            foreach (FileOffset file in file_offsets)
            {
                writer.Write(intToBytes((int)file.offset));
                writer.Write(intToBytes((int)file.size));
            }

            sbyte nothing = 0;

            writer.Write(stringToBytes("TNNM"));
            writer.Write(intToBytes(string_section_size));
            foreach (FileEntry file in files)
            {
                writer.Write(stringToBytes(file.toneName));
                writer.Write(nothing);
            }

            writer.Write(stringToBytes("JUNK"));
            writer.Write(intToBytes(junk_pad));
            for (var i = 0; i < junk_pad; i++)
                writer.Write(nothing);

            writer.Write(stringToBytes("PACK"));
            writer.Write(intToBytes(pack_section_size));

            FileEntry[] files_to_pack_arr = files_to_pack.ToArray();

            if (files_arr.Length == 1)
                writer.Write(files[0].fileData);
            else
                for (var i = 0; i < files_to_pack_arr.Length; i++)
                {
                    writer.Write(files_to_pack_arr[i].fileData);
                    int fill = (int)(writer.BaseStream.Position % 16);
                    for (var x = 0; x < fill; x++)
                        writer.Write(nothing);
                }

            writer.Close();

            return true;
        }

        public bool Write(string path)
        {
            BinaryWriter writer = new BinaryWriter(File.OpenWrite(path));
            return Write(writer);
        }

        public bool Add(string toneName, byte[] fileData)
        {
            FileEntry entry = new FileEntry();
            entry.toneName = toneName;
            entry.fileData = fileData;
            files.Add(entry);
            return true;
        }

        public bool Add(string toneName, string path)
        {
            if (File.Exists(path))
                return Add(toneName, File.ReadAllBytes(path));
            else
                return false;
        }

        public FileEntry getFileByToneName(string toneName)
        {
            return files.Find(x => x.toneName == toneName);
        }

        public bool Extract(int id)
        {
            string output = files[id].toneName;
            byte[] song_data = files[id].fileData;

            if(song_data.Length > 0)
            {
                string magic = GetMagic(song_data);
                if (magic == "IDSP")
                    output += ".idsp";
                else if (magic == "OPUS")
                    output += ".lopus";
            }
            BinaryWriter write = new BinaryWriter(File.OpenWrite(output));
            write.Write(song_data);
            return true;
        }
    }
}
