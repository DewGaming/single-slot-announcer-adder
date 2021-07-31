using DamienG.Security.Cryptography;
using Microsoft.Scripting.Hosting;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Nus3Audio_Editor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// The nus3audio variable for the inputted nus3audio file.
        /// </summary>
        private nus3audio nus3audio;

        /// <summary>
        /// The file path for the nus3audio file.
        /// </summary>
        private string nus3audioPath;

        /// <summary>
        /// The OpenFileDialog variable for nus3bank files.
        /// </summary>
        private OpenFileDialog nus3bankFile = new OpenFileDialog();

        /// <summary>
        /// The OpenFileDialog variable for sli files.
        /// </summary>
        private OpenFileDialog sliFile = new OpenFileDialog();

        /// <summary>
        /// The OpenFileDialog variable for the audio files.
        /// </summary>
        private OpenFileDialog audioFile = new OpenFileDialog();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Nus3audioFileBrowserButton_Click(object sender, RoutedEventArgs e)
        {
            // Set filter for file extension and default file extension 
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.DefaultExt = ".nus3audio";
            fileDialog.Filter = "nus3audio files (*.nus3audio)|*.nus3audio";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = fileDialog.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Creates a nus3audio variable with the given file path.
                nus3audioPath = fileDialog.FileName;
                nus3audio = new nus3audio(nus3audioPath);
                // Sets the textbox text to that of the file name.
                nus3audioFileTextBox.Text = fileDialog.SafeFileName;
                // Sets the textbox text for the Entry ID to the count of nus3audio files.
                entryIDTextBox.Text = nus3audio.files.Count().ToString();
            }
        }

        private void Nus3bankFileBrowserButton_Click(object sender, RoutedEventArgs e)
        {
            // Set filter for file extension and default file extension 
            nus3bankFile.DefaultExt = ".nus3bank";
            nus3bankFile.Filter = "nus3bank files (*.nus3bank)|*.nus3bank";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = nus3bankFile.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document
                nus3bankFileTextBox.Text = nus3bankFile.SafeFileName;
            }
        }

        private void SliFileBrowserButton_Click(object sender, RoutedEventArgs e)
        {
            // Set filter for file extension and default file extension 
            sliFile.DefaultExt = ".sli";
            sliFile.Filter = "sli files (*.sli)|*.sli";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = sliFile.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document
                sliFileTextBox.Text = sliFile.SafeFileName;
            }
        }

        private void AudioFileButton_Click(object sender, RoutedEventArgs e)
        {
            // Set filter for file extension and default file extension 
            audioFile.DefaultExt = ".idsp";
            audioFile.Filter = "IDSP files (*.idsp)|*.idsp";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = audioFile.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document
                audioFileTextBox.Text = audioFile.SafeFileName;
            }
        }

        private void AddSoundEffectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ensures that everything that is required has been inputted.
                if (newSoundEffectTextBox.Text != "vc_narration_characall_" && !String.IsNullOrEmpty(sliFile.FileName) && !String.IsNullOrEmpty(nus3bankFile.FileName) && !String.IsNullOrEmpty(newSoundEffectTextBox.Text) && nus3audio != null)
                {
                    // Creates a list of all the entry names in the nus3audio file.
                    List<string> nus3audioEntries = new List<string>();
                    foreach (var entry in nus3audio.files)
                    {
                        nus3audioEntries.Add(entry.toneName);
                    }

                    #region NUS3Audio
                    if (String.IsNullOrEmpty(audioFile.FileName))
                    {
                        // Adds the entry to the nus3audio without any data.
                        nus3audio.Add(newSoundEffectTextBox.Text, new byte[0]);
                    }
                    else
                    {
                        // Adds the entry to the nus3audio with the audio file's data.
                        nus3audio.Add(newSoundEffectTextBox.Text, audioFile.FileName);
                    }
                    #endregion

                    #region NUS3Bank
                    byte[] nus3bankBytes = File.ReadAllBytes(nus3bankFile.FileName);
                    int skipCount = 4;
                    uint toneOffset = 0, toneSize = 0;
                    UInt32 nus3bankSize = (UInt32)StructConverter.Unpack("I", ref skipCount, nus3bankBytes.ToArray())[0];
                    // Ensures that the nus3bank file is valid
                    if (System.Text.Encoding.UTF8.GetString(nus3bankBytes.Skip(skipCount).Take(8).ToArray()) != "BANKTOC ")
                    {
                        MessageBox.Show("Invalid nus3bank file!");
                    }
                    else
                    {
                        skipCount += 8;
                        UInt32 tocSize = (UInt32)StructConverter.Unpack("I", ref skipCount, nus3bankBytes.ToArray())[0];
                        UInt32 contentCount = (UInt32)StructConverter.Unpack("I", ref skipCount, nus3bankBytes.ToArray())[0];
                        UInt32 offset = tocSize + 20;
                        int toneHeaderOffset = 0;
                        // Gets the offset of the section "TONE".
                        for (var i = 0; i < contentCount; i++)
                        {
                            byte[] content = nus3bankBytes.Skip(skipCount).Take(4).ToArray();
                            skipCount += 4;
                            toneHeaderOffset = skipCount;
                            UInt32 contentSize = (UInt32)StructConverter.Unpack("I", ref skipCount, nus3bankBytes.ToArray())[0];
                            if (System.Text.Encoding.UTF8.GetString(content) == "TONE")
                            {
                                toneOffset = offset;
                                toneSize = contentSize;
                                break;
                            }

                            offset += 8 + contentSize;
                        }

                        skipCount = (int)toneOffset + 8;
                        UInt32 toneCount = (UInt32)StructConverter.Unpack("I", ref skipCount, nus3bankBytes.ToArray())[0];
                        int comparableEntryOffset = Search(nus3bankBytes, Encoding.ASCII.GetBytes("vc_narration_characall_mario"));
                        skipCount = comparableEntryOffset;

                        while (skipCount % 4 != 0)
                        {
                            skipCount++;
                        }

                        while ((UInt32)StructConverter.Unpack("I", ref skipCount, nus3bankBytes.ToArray())[0] != 8936) { }

                        UInt32 starting_position = (UInt32)skipCount, value;
                        int break_counter = 0;
                        while (true)
                        {
                            value = (UInt32)StructConverter.Unpack("I", ref skipCount, nus3bankBytes.ToArray())[0];
                            if (break_counter % 2 == 0)
                            {
                                if (value == 0)
                                {
                                    break_counter++;
                                }
                                else
                                {
                                    break_counter = 0;
                                }
                            }
                            else
                            {
                                if (value == 4294967295)
                                {
                                    break_counter++;
                                }
                                else
                                {
                                    break_counter = 0;
                                }
                            }

                            if (break_counter == 8)
                            {
                                break;
                            }
                        }

                        UInt32 comparableSize = (UInt32)skipCount - starting_position;

                        skipCount = comparableEntryOffset;
                        value = (UInt32)StructConverter.Unpack("I", ref skipCount, nus3bankBytes.ToArray())[0];
                        while (skipCount % 4 != 0)
                        {
                            skipCount++;
                        }

                        while ((UInt32)StructConverter.Unpack("I", ref skipCount, nus3bankBytes.ToArray())[0] != 8936) { }

                        byte[] comparableMetaData = nus3bankBytes.Skip(skipCount).Take((int)comparableSize).ToArray();
                        skipCount = comparableEntryOffset - 13;
                        byte[] comparablePreMetaData = nus3bankBytes.Skip(skipCount).Take(12).ToArray();
                        skipCount = (int)toneOffset + 12 + ((int)toneCount - 1) * 8;
                        UInt32 lastToneOffset = (UInt32)StructConverter.Unpack("I", ref skipCount, nus3bankBytes.ToArray())[0];
                        UInt32 lastToneSize = (UInt32)StructConverter.Unpack("I", ref skipCount, nus3bankBytes.ToArray())[0];
                        UInt32 newToneSize = comparableSize + 29 + (UInt32)newSoundEffectTextBox.Text.Length;
                        newToneSize += 4 - ((UInt32)newSoundEffectTextBox.Text.Length + 1) % 4;
                        skipCount = 0;
                        #endregion

                        #region sli
                        byte[] sliBytes = File.ReadAllBytes(sliFile.FileName);
                        skipCount = 8;
                        UInt32 sliCount = (UInt32)StructConverter.Unpack("I", ref skipCount, sliBytes.ToArray())[0];
                        UInt64 newHash = (UInt64)(newSoundEffectTextBox.Text.Length * Math.Pow(2, 32)) | Crc32.Compute(Encoding.ASCII.GetBytes(newSoundEffectTextBox.Text));
                        UInt64 comparableHash = (UInt64)("vc_narration_characall_mario".Length * Math.Pow(2, 32)) | Crc32.Compute(Encoding.ASCII.GetBytes("vc_narration_characall_mario"));
                        int newOffset = 0, comparableOffset = 0;
                        bool hashExists = false;
                        for (var i = 0; i <= sliCount; i++)
                        {
                            skipCount = 16 * i + 12;
                            UInt64 hash40 = (UInt64)ReadUInt64(sliBytes, ref skipCount);
                            if (hash40 == (UInt64)comparableHash)
                            {
                                comparableOffset = i;
                            }
                            else if (hash40 == newHash)
                            {
                                hashExists = true;
                                break;
                            }
                            else if (hash40 > newHash && newOffset == 0)
                            {
                                newOffset = i;
                            }

                            if (comparableOffset != 0 && newOffset != 0)
                            {
                                break;
                            }
                        }

                        if (comparableHash == 0)
                        {
                            MessageBox.Show("The comparable sound label could not be found in the table!");
                        }
                        else if (hashExists)
                        {
                            MessageBox.Show("The new sound label already exists in the table!");
                        }
                        else
                        {
                            if (newOffset == 0)
                            {
                                newOffset = (int)sliCount;
                            }
                            #endregion

                            #region writeToFiles
                            #region writeNus3audio
                            nus3audio.Write(nus3audioPath);
                            #endregion
                            
                            #region writeNus3bank
                            List<byte> newNus3bankBytes = new List<byte>();
                            newNus3bankBytes.AddRange(Encoding.ASCII.GetBytes("NUS3"));
                            newNus3bankBytes.AddRange(BitConverter.GetBytes(nus3bankSize + newToneSize + 8));
                            newNus3bankBytes.AddRange(nus3bankBytes.Skip(8).Take(toneHeaderOffset - 8));
                            newNus3bankBytes.AddRange(BitConverter.GetBytes(toneSize + newToneSize + 8));
                            newNus3bankBytes.AddRange(nus3bankBytes.Skip(toneHeaderOffset + 4).Take((int)toneOffset - toneHeaderOffset));
                            newNus3bankBytes.AddRange(BitConverter.GetBytes(toneSize + newToneSize + 8));
                            newNus3bankBytes.AddRange(BitConverter.GetBytes(toneCount + 1));
                            for (var count = 0; count < toneCount; count++)
                            {
                                uint curToneOffset = (UInt32)StructConverter.Unpack("I", (int)toneOffset + 12 + count * 8, nus3bankBytes.ToArray())[0];
                                byte[] modToneOffset = BitConverter.GetBytes(curToneOffset + 8);
                                newNus3bankBytes.AddRange(modToneOffset);
                                newNus3bankBytes.AddRange(nus3bankBytes.Skip((int)toneOffset + 16 + count * 8).Take(4).ToArray());
                            }
                            
                            newNus3bankBytes.AddRange(BitConverter.GetBytes(lastToneOffset + lastToneSize + 8));
                            newNus3bankBytes.AddRange(BitConverter.GetBytes(newToneSize));
                            newNus3bankBytes.AddRange(nus3bankBytes.Skip((int)toneOffset + 12 + (int)toneCount * 8).Take(((int)lastToneOffset + (int)lastToneSize) - 4 - (int)toneCount * 8));
                            newNus3bankBytes.AddRange(comparablePreMetaData);
                            newNus3bankBytes.Add((byte)(newSoundEffectTextBox.Text.Length + 1));
                            newNus3bankBytes.AddRange(Encoding.ASCII.GetBytes(newSoundEffectTextBox.Text));
                            int counter = newSoundEffectTextBox.Text.Length + 1;
                            if (counter % 4 == 0)
                            {
                                newNus3bankBytes.AddRange(BitConverter.GetBytes(0));
                            }
                            
                            while (counter %4 != 0)
                            {
                                newNus3bankBytes.Add((byte)0);
                                counter++;
                            }
                            
                            newNus3bankBytes.AddRange(BitConverter.GetBytes(0));
                            newNus3bankBytes.AddRange(BitConverter.GetBytes(8));
                            newNus3bankBytes.AddRange(BitConverter.GetBytes(0));
                            newNus3bankBytes.AddRange(BitConverter.GetBytes(8936));
                            newNus3bankBytes.AddRange(comparableMetaData);
                            newNus3bankBytes.AddRange(nus3bankBytes.Skip((int)(toneOffset + 8 + lastToneOffset + lastToneSize)).ToArray());
                            File.WriteAllBytes(nus3bankFile.FileName, newNus3bankBytes.ToArray());
                            #endregion

                            #region writeSli
                            byte[] comparableData = sliBytes.Skip(12 + 16 * comparableOffset + 8).Take(4).ToArray();
                            List<byte> newSliBytes = new List<byte>();
                            newSliBytes.AddRange(Encoding.ASCII.GetBytes("SLI"));
                            newSliBytes.Add((byte)0);
                            newSliBytes.AddRange(BitConverter.GetBytes(1));
                            newSliBytes.AddRange(BitConverter.GetBytes(sliCount + 1));
                            if (newOffset == sliCount)
                            {
                                newSliBytes.AddRange(sliBytes.Skip(12).ToArray());
                                newSliBytes.AddRange(BitConverter.GetBytes(newHash));
                                newSliBytes.AddRange(comparableData);
                                newSliBytes.AddRange(BitConverter.GetBytes(int.Parse(entryIDTextBox.Text)));
                            }
                            else
                            {
                                newSliBytes.AddRange(sliBytes.Skip(12).Take(16 * newOffset).ToArray());
                                newSliBytes.AddRange(BitConverter.GetBytes(newHash));
                                newSliBytes.AddRange(comparableData);
                                newSliBytes.AddRange(BitConverter.GetBytes(int.Parse(entryIDTextBox.Text)));
                                newSliBytes.AddRange(sliBytes.Skip(12 + 16 * newOffset).ToArray());
                            }

                            File.WriteAllBytes(sliFile.FileName, newSliBytes.ToArray());
                            #endregion
                            MessageBox.Show("Files have been modified! Copy this hash (0x" + newHash.ToString("X") + ") for use in ParamXML");
                            // Sets the textbox text for the Entry ID to new the count of nus3audio files.
                            entryIDTextBox.Text = nus3audio.files.Count().ToString();
                            #endregion
                        }
                    }
                }
                else if (newSoundEffectTextBox.Text == "vc_narration_characall_")
                {
                    MessageBox.Show("You need to modify the New Sound Effect Name text before continuing!");
                }
                else
                {
                    MessageBox.Show("There are required parts that have not been done yet!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private static ulong ReadUInt64(byte[] bytes, ref int skipCount)
        {
            var rawBytes = bytes.Skip(skipCount).Take(8).ToArray();

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(rawBytes);
            }

            skipCount += 8;

            return BitConverter.ToUInt64(rawBytes, 0);
        }

        private int Search(byte[] src, byte[] pattern)
        {
            int maxFirstCharSlot = src.Length - pattern.Length + 1;
            for (int i = 0; i < maxFirstCharSlot; i++)
            {
                if (src[i] != pattern[0]) // compare only first byte
                    continue;

                // found a match on first byte, now try to match rest of the pattern
                for (int j = pattern.Length - 1; j >= 1; j--)
                {
                    if (src[i + j] != pattern[j]) break;
                    if (j == 1) return i;
                }
            }
            return -1;
        }
    }
}
