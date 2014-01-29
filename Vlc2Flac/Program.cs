/*
 * Program Title: Vlc2Flac
 * Version: 0.9.0.1
 * Author: Joseph Cassano (http://jplc.ca)
 * Year: 2014
 * Description:
 * 		Interface for using the vlc, FlacFixer, and
 * 		metaflac programs to create a proper FLAC
 * 		file from a VLC-compatible media file.
 * 		File paths for the vlc, FlacFixer, and
 * 		metaflac programs are stored in a config.xml
 * 		file in the same directory as the executable
 * 		for Vlc2Flac.
 * License:
 * 		MIT License (see LICENSE.txt in the project's root
 * 		directory for details).
 * Target Framework:
 * 		Mono / .NET 4.0
 * References:
 * 		atk-sharp
 * 		gdk-sharp
 * 		glib-sharp
 * 		gtk-sharp
 * 		System
 * 		System.Xml
 * External programs used in this program:
 * 		vlc
 * 		FlacFixer (created by me; can be found on GitHub)
 * 		metaflac
 * Confirmed Compatibility:
 * 		Windows 7 64-bit
 */

using Gtk;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Serialization;

namespace Vlc2Flac
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			Console.OutputEncoding = System.Text.Encoding.UTF8;
			//args = new string[]{"-gui", "-c"};//, "-ucl"}; // Test arguments
			RunClass.Run(args);
		}
	}

	public static class RunClass
	{
		public static string filePath;
		public static bool fixerForceOverwrite = false;
		public static bool fixerKeepTemp = false;
		public static bool deleteOriginal = false;

		public static bool useGUI = false;
		public static bool showConsole = false;
		public static bool useCustomLoop = false;

		[DllImport("kernel32.dll", EntryPoint = "GetConsoleWindow")]
		private static extern IntPtr GetConsoleWindow();

		[DllImport("user32.dll", EntryPoint = "ShowWindow")]
		private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		static RunClass()
		{
			fixerForceOverwrite = false;
			fixerKeepTemp = false;
			deleteOriginal = false;

			useGUI = false;
			showConsole = false;
			useCustomLoop = false;
		}

		public static void Run(string[] args)
		{
			Console.WriteLine("Vlc2Flac STARTED!");
			if (args.Length > 0)
			{
				filePath = args[0];

				foreach (string arg in args)
				{
					if (arg == "-ff")
					{
						fixerForceOverwrite = true;
					}
					else if (arg == "-fkt")
					{
						fixerKeepTemp = true;
					}
					else if (arg == "-do")
					{
						deleteOriginal = true;
					}
					else if (arg == "-gui")
					{
						useGUI = true;
					}
					else if (arg == "-c")
					{
						showConsole = true;
					}
					else if (arg == "-ucl")
					{
						useCustomLoop = true;
					}
				}

				if (!useGUI)
				{
					ConfigManager.DeserializeFromXml();
					VLCHandler.Run(filePath, fixerForceOverwrite, fixerKeepTemp, deleteOriginal);
				}
			}
			else
			{
				useGUI = true;
			}

			if (useGUI)
			{
				IntPtr h = GetConsoleWindow();
				if (!showConsole)
				{
					ShowWindow(h, 0);
				}

				Application.Init();
				CustomMainWindow myWin = new CustomMainWindow("Vlc2Flac", !useCustomLoop);

				myWin.ShowAll();
				myWin.Run();
				if (useCustomLoop)
				{
					while (myWin.IsRunning)
					{
						Main.IterationDo(false);
						myWin.WriteConsoleMessages();
					}
				}
				else
				{
					Application.Run();
				}
				myWin.Destroy();

				if (!showConsole)
				{
					ShowWindow(h, 1);
				}
			}
			Console.WriteLine("Vlc2Flac DONE!");
			//Console.Write("Press any key to close... ");
			//Console.ReadKey();
			//Console.Write("\n");
		}
	}

	public static class ConfigManager
	{
		public static string exeVlcPath{ get; private set; }
		public static string exeFlacFixerPath{ get; private set; }
		public static string exeMetaFlacPath{ get; private set; }

		private static string xmlPath;
		private static Config currentConfig;
		private static Config CurrentConfig
		{
			get
			{
				return currentConfig;
			}
			set
			{
				currentConfig = value;
				exeVlcPath = currentConfig.exeVlcPath;
				exeFlacFixerPath = currentConfig.exeFlacFixerPath;
				exeMetaFlacPath = currentConfig.exeMetaFlacPath;
			}
		}

		static ConfigManager()
		{
			xmlPath = String.Concat(new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location).DirectoryName, @"\config.xml");
			CurrentConfig = new Config();
		}

		static public void SerializeToXml()
		{
			XmlSerializer serializer = new XmlSerializer(typeof(Config));
			TextWriter textWriter = new StreamWriter(xmlPath, false, System.Text.Encoding.UTF8);
			serializer.Serialize(textWriter, CurrentConfig);
			textWriter.Close();
		}

		static public void DeserializeFromXml()
		{
			if (!File.Exists(xmlPath))
			{
				Config tempConfig = new Config();
				if (CurrentConfig != tempConfig)
				{
					CurrentConfig = tempConfig;
				}
				SerializeToXml();
			}
			else
			{
				XmlSerializer deserializer = new XmlSerializer(typeof(Config));
				TextReader textReader = new StreamReader(xmlPath, System.Text.Encoding.UTF8);
				CurrentConfig = (Config)deserializer.Deserialize(textReader);
				textReader.Close();
			}
		}

		public class Config
		{
			public string exeVlcPath;
			public string exeFlacFixerPath;
			public string exeMetaFlacPath;

			public Config()
			{
				exeVlcPath = @"C:\Program Files (x86)\VideoLAN\VLC\vlc.exe";
				exeFlacFixerPath = @"C:\Program Files (x86)\FlacFixer\FlacFixer\FlacFixer.exe";
				exeMetaFlacPath = @"C:\Program Files (x86)\FLAC Frontend\tools\metaflac.exe";
			}

			public override bool Equals(object obj)
			{
				if (obj == null)
				{
					return false;
				}

				Config config = obj as Config;
				/*				
				if ((object)config == null)
				{
					return false;
				}

				return (exeVlcPath == config.exeVlcPath) && (exeFlacFixerPath == config.exeFlacFixerPath) && (exeMetaFlacPath == config.exeMetaFlacPath);
				*/
				return Equals(config);
			}

			public bool Equals(Config config)
			{
				if ((object)config == null)
				{
					return false;
				}

				return (exeVlcPath == config.exeVlcPath) && (exeFlacFixerPath == config.exeFlacFixerPath) && (exeMetaFlacPath == config.exeMetaFlacPath);
			}

			public override int GetHashCode()
			{
				return exeVlcPath.GetHashCode() ^ exeFlacFixerPath.GetHashCode() ^ exeMetaFlacPath.GetHashCode();
			}

			public static bool operator ==(Config configA, Config configB)
			{
				if (object.ReferenceEquals(configA, configB))
				{
					return true;
				}

				if (((object)configA == null) || ((object)configB == null))
				{
					return false;
				}

				return (configA.exeVlcPath == configB.exeVlcPath) && (configA.exeFlacFixerPath == configB.exeFlacFixerPath) && (configA.exeMetaFlacPath == configB.exeMetaFlacPath);
			}

			public static bool operator !=(Config configA, Config configB)
			{
				return !(configA == configB);
			}
		}
	}

	public static class VLCHandler
	{
		private const string ExtFlac = ".flac";

		private static Process process;
		private static ProcessStartInfo startInfo;
		private static FileInfo exeFlacFixer;
		private static List<string> outputList;
		private static string finalFilePath;

		static VLCHandler()
		{
			process = new Process();
			startInfo = new ProcessStartInfo();
			exeFlacFixer = new FileInfo(ConfigManager.exeFlacFixerPath);
			finalFilePath = "";
			outputList = new List<string>();
		}

		public static string Run(string initialFilePath, bool fixerForceOverwrite, bool fixerKeepTemp, bool deleteOriginal)
		{
			Console.WriteLine("VLC Handler started.");
			if ((initialFilePath != "") && (initialFilePath != null))
			{
				FileInfo initialFile = new FileInfo(initialFilePath);
				if (File.Exists(initialFile.FullName))
				{
					startInfo.Verb = "runas";
					startInfo.WindowStyle = ProcessWindowStyle.Hidden;
					startInfo.RedirectStandardOutput = true;
					startInfo.UseShellExecute = false;
					startInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
					startInfo.FileName = String.Concat(Environment.ExpandEnvironmentVariables("%SystemRoot%"), @"\System32\cmd.exe");
					FileInfo outputTempFile = new FileInfo(String.Concat(initialFile.DirectoryName, @"\VLC_Output.flac"));
					startInfo.Arguments = String.Concat("/C \"\"", ConfigManager.exeVlcPath, "\" -I dummy -vvv \"", initialFile.FullName, "\" --no-sout-video --sout-audio --no-sout-rtp-sap --no-sout-standard-sap --ttl=1 --sout-keep --sout=#transcode{acodec=flac}:std{mux=raw,dst=\"", outputTempFile.FullName, "\"} vlc://quit\"");
					process.StartInfo = startInfo;
					process.Start();
					process.WaitForExit();
					process.Close();
					byte[] byteArray = File.ReadAllBytes(outputTempFile.FullName);
					if (deleteOriginal)
					{
						File.Delete(initialFile.FullName);
					}
					string outputPath;
					int initExtensionLength = initialFile.Extension.Length;
					int initPathLength = initialFile.FullName.Length;
					if (initialFile.Extension != ExtFlac)
					{
						outputPath = String.Concat(initialFile.FullName.Substring(0, (initPathLength - initExtensionLength)), ExtFlac);
					}
					else
					{
						if (deleteOriginal)
						{
							outputPath = initialFile.FullName;
						}
						else
						{
							outputPath = String.Concat(initialFile.FullName.Substring(0, (initPathLength - initExtensionLength)), "_NEW", ExtFlac);
						}
					}
					FileInfo outputFile = new FileInfo(outputPath);
					File.Delete(outputTempFile.FullName);
					File.WriteAllBytes(outputFile.FullName, byteArray);

					string fixerOverwriteString;
					if (fixerForceOverwrite)
					{
						fixerOverwriteString = " -f";
					}
					else
					{
						fixerOverwriteString = "";
					}
					string fixerKeepTempString;
					if (fixerKeepTemp)
					{
						fixerKeepTempString = " -kt";
					}
					else
					{
						fixerKeepTempString = "";
					}
					startInfo.Arguments = String.Concat("/C \"\"", exeFlacFixer.FullName, "\" \"", outputFile.FullName, "\"",
						fixerOverwriteString, fixerKeepTempString, " -do\"");
					process.StartInfo = startInfo;
					string tempOutput = "";
					process.Start();
					while (!process.HasExited)
					{
						tempOutput = process.StandardOutput.ReadToEnd();
					}
					process.WaitForExit();
					process.Close();
					outputList = new List<string>(tempOutput.Split('\n'));
					string checkString = "Output File: ";
					foreach (string line in outputList)
					{
						if (line != null)
						{
							string trimmedLine = line.Trim();
							if (trimmedLine.StartsWith(checkString))
							{
								finalFilePath = trimmedLine.Substring(checkString.Length);
							}
						}
					}
				}
			}
			Console.WriteLine("VLC Handler complete.");
			return finalFilePath;
		}
	}

	public static class TagHandler
	{
		public static List<bool> removeList;
		public static List<bool> addList;
		public static List<string> nameList;
		public static List<string> valueList;

		private const string OpRemoveTag = " --remove-tag=";
		private const string OpAddTag = " --set-tag=";

		private static Process process;
		private static ProcessStartInfo startInfo;
		private static FileInfo editFile;
		private static List<string> outputList;

		static TagHandler()
		{
			removeList = new List<bool>();
			addList = new List<bool>();
			nameList = new List<string>();
			valueList = new List<string>();

			process = new Process();
			startInfo = new ProcessStartInfo();
		}

		public static List<string> Run(string filePath, string newName)
		{
			Console.WriteLine("Tag Handler started.");
			outputList = new List<string>();
			if ((filePath != "") && (filePath != null))
			{
				editFile = new FileInfo(filePath);
				if (File.Exists(editFile.FullName) && editFile.FullName.EndsWith(".flac"))
				{
					startInfo.Verb = "runas";
					startInfo.WindowStyle = ProcessWindowStyle.Hidden;
					startInfo.RedirectStandardOutput = true;
					startInfo.UseShellExecute = false;
					startInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
					startInfo.FileName = String.Concat(Environment.ExpandEnvironmentVariables("%SystemRoot%"), @"\System32\cmd.exe");
					string initialCommand = String.Concat("/C \"\"", ConfigManager.exeMetaFlacPath, "\" \"", editFile.FullName, "\" --no-utf8-convert");
					string command = initialCommand;
					for (int i = 0; i < nameList.Count; i++)
					{
						if ((nameList[i] != null) && (nameList[i] != ""))
						{
							if (removeList[i])
							{
								command = String.Concat(command, OpRemoveTag, "\"", nameList[i], "\"");
							}

							if ((valueList[i] != null) && (valueList[i] != ""))
							{
								if (addList[i])
								{
									command = String.Concat(command, OpAddTag, "\"", nameList[i], "=", valueList[i], "\"");
								}
							}
						}
					}
					if (initialCommand != command)
					{
						command = String.Concat(command, "\"");
						startInfo.Arguments = command;
						process.StartInfo = startInfo;
						process.Start();
						process.WaitForExit();
						process.Close();
					}

					startInfo.Arguments = String.Concat(initialCommand, " --list\"");
					process.StartInfo = startInfo;
					string tempOutput = "";
					process.Start();
					while (!process.HasExited)
					{
						tempOutput = process.StandardOutput.ReadToEnd();
					}
					process.WaitForExit();
					process.Close();
					outputList = new List<string>(tempOutput.Split('\n'));
					foreach (string line in outputList)
					{
						Console.WriteLine(line);
					}

					if ((newName != null) && (newName != ""))
					{
						string initialNewName = newName;
						newName = String.Concat(newName, ".flac");
						if (newName != editFile.Name)
						{
							string newFilePath = String.Concat(editFile.DirectoryName, @"\", newName);
							if (File.Exists(newFilePath))
							{
								newFilePath = String.Concat(editFile.DirectoryName, @"\", initialNewName, "_NEW.flac");
							}
							File.Move(editFile.FullName, newFilePath);
						}
					}
				}
			}
			Console.WriteLine("Tag Handler complete.");
			return outputList;
		}
	}

	public class MainWindow : Window
	{
		private bool isRunning;
		public bool IsRunning
		{
			get
			{
				return isRunning;
			}
			private set
			{
				isRunning = value;
			}
		}

		private List<string[]> consoleOutputList;

		public MainWindow(string title, bool quitOnDelete = true) : base(title)
		{
			Console.WriteLine("Creating Main Window.");
			consoleOutputList = new List<string[]>();
			IsRunning = false;
			DeleteEvent += new DeleteEventHandler((curSender, curE) => MainWindow_Delete(curSender, curE, quitOnDelete));
			Console.WriteLine("Main Window created.");
		}

		public void Run()
		{
			if (!IsRunning)
			{
				IsRunning = true;
			}
		}

		public void ConsoleWriteLine(string message)
		{
			ConsoleWriter(message, "true");
		}

		public void ConsoleWrite(string message)
		{
			ConsoleWriter(message, "false");
		}

		public void WriteConsoleMessages()
		{
			if (consoleOutputList.Count > 0)
			{
				foreach (string[] stringArray in consoleOutputList)
				{
					bool doWriteLine;
					if (bool.TryParse(stringArray[1], out doWriteLine))
					{
						if (doWriteLine)
						{
							Console.WriteLine(stringArray[0]);
						}
						else
						{
							Console.Write(stringArray[0]);
						}
					}
				}
				consoleOutputList.Clear();
			}
		}

		private void ConsoleWriter(string message, string doWriteLine)
		{
			consoleOutputList.Add(new string[]{message, doWriteLine});
		}

		private void MainWindow_Delete(object sender, DeleteEventArgs e, bool quitOnDelete)
		{
			Console.WriteLine("Closing Main Window.");
			if (quitOnDelete)
			{
				Application.Quit();
			}
			e.RetVal = true;
			if (IsRunning)
			{
				IsRunning = false;
			}
			Console.WriteLine("Main Window closed.");
		}
	}

	public class CustomMainWindow : MainWindow
	{
		// General purpose widget variables.
		public EventBox eventBox;
		public VBox vBox;
		public Label label;
		public Entry entry;
		public HBox hBox;
		public Button button;
		public FileChooserButton fileChooserButton;
		public CheckButton checkButton;
		public TextTag textTag;
		public TextTagTable textTagTable;
		public TextBuffer textBuffer;
		public TextView textView;

		// Specific widget variables.
		public HBox button_hBox;
		public VBox tag_vBox;
		public CheckButton conversion_checkButton;
		public CheckButton tagEdit_checkButton;
		public CheckButton tagRemoveAll_checkButton;
		public CheckButton tagAddAll_checkButton;
		public Entry newName_entry;

		public List<HBox> entry_hBoxList;
		public List<List<Entry>> tag_entryList;
		public List<List<CheckButton>> tag_checkButtonList;

		public CustomMainWindow(string title, bool quitOnDelete = true) : base(title, quitOnDelete)
		{
			Console.WriteLine("Creating Custom Main Window.");
			SetDefaultSize(720,480);
			entry_hBoxList = new List<HBox>();
			tag_entryList = new List<List<Entry>>();
			tag_checkButtonList = new List<List<CheckButton>>();
			Console.WriteLine("Custom Main Window created.");
			InitContents();
		}

		public void InitContents()
		{
			eventBox = new EventBox();
			eventBox.CanFocus = true;
			eventBox.CanDefault = true;
			Add(eventBox);

			vBox = new VBox();
			eventBox.Add(vBox);

			label = new Label();
			label.Text = "Please choose a VLC-compatible file";
			vBox.Add(label);

			fileChooserButton = new FileChooserButton(label.Text, FileChooserAction.Open);
			vBox.Add(fileChooserButton);

			label = new Label();
			label.Text = "FLAC tag editing options";
			vBox.Add(label);

			hBox = new HBox();
			vBox.Add(hBox);

			tagRemoveAll_checkButton = new CheckButton();
			tagRemoveAll_checkButton.Label = "Select all for removal";
			tagRemoveAll_checkButton.Clicked += new EventHandler((curSender, curE) => CheckButton_Clicked_SelectAll(curSender, curE, tagRemoveAll_checkButton, 0));
			hBox.Add(tagRemoveAll_checkButton);

			label = new Label();
			label.Text = "Tag name";
			hBox.Add(label);

			label = new Label();
			label.Text = "Tag value";
			hBox.Add(label);

			tagAddAll_checkButton = new CheckButton();
			tagAddAll_checkButton.Label = "Select all for addition";
			tagAddAll_checkButton.Clicked += new EventHandler((curSender, curE) => CheckButton_Clicked_SelectAll(curSender, curE, tagAddAll_checkButton, 1));
			hBox.Add(tagAddAll_checkButton);

			tag_vBox = new VBox();
			vBox.Add(tag_vBox);

			List<string> tagTitleList = new List<string>(){"TITLE", "ARTIST", "ALBUMARTIST", "ALBUM", "TRACKNUMBER"};
			foreach (string tagTitle in tagTitleList)
			{
				AddTagEntryRow(false, tagTitle);
			}

			hBox = new HBox();
			button_hBox = hBox;
			vBox.Add(hBox);

			button = new Button();
			button.Label = "Add Fields";
			button.Clicked += new EventHandler(Button_Clicked_AddRow);
			hBox.Add(button);

			button = new Button();
			button.Label = "Remove Fields";
			button.Clicked += new EventHandler(Button_Clicked_DestroyRow);
			hBox.Add(button);

			conversion_checkButton = new CheckButton();
			conversion_checkButton.Label = "Convert from vlc.exe-compatible file to FLAC file";
			vBox.Add(conversion_checkButton);

			tagEdit_checkButton = new CheckButton();
			tagEdit_checkButton.Label = "Edit tags with metaflac.exe (tag removal first, tag addition second)";
			vBox.Add(tagEdit_checkButton);

			label = new Label();
			label.Text = "Name of output file (without extension) (leave blank for same name as input file):";
			vBox.Add(label);

			newName_entry = new Entry();
			vBox.Add(newName_entry);

			button = new Button();
			button.Label = "Run";
			button.Clicked += new EventHandler(Button_Clicked_Run);
			vBox.Add(button);

			label = new Label();
			label.Text = "Output Information";
			vBox.Add(label);

			textTagTable = new TextTagTable();

			textBuffer = new TextBuffer(textTagTable);

			textView = new TextView();
			textView.Buffer = textBuffer;
			textView.Editable = false;
			vBox.Add(textView);
		}

		private void Button_Clicked_AddRow(object sender, EventArgs e)
		{
			AddTagEntryRow(true);
		}

		private void AddTagEntryRow(bool manualShow = false, string tagName = "", string tagValue = "")
		{
			hBox = new HBox();
			entry_hBoxList.Add(hBox);
			tag_vBox.Add(hBox);
			if (manualShow)
			{
				hBox.Show();
			}

			tag_checkButtonList.Add(new List<CheckButton>());
			AddTagCheckButton(manualShow);

			List<string> entryTextList = new List<string>(){tagName, tagValue};
			tag_entryList.Add(new List<Entry>());
			foreach (string entryText in entryTextList)
			{
				AddTagEntry(manualShow, entryText);
			}

			AddTagCheckButton(manualShow);
		}

		private void AddTagEntry(bool manualShow = false, string tagText = "")
		{
			entry = new Entry();
			entry.Text = tagText;
			tag_entryList[tag_entryList.Count - 1].Add(entry);
			hBox.Add(entry);
			if (manualShow)
			{
				entry.Show();
			}
		}

		private void AddTagCheckButton(bool manualShow = false)
		{
			checkButton = new CheckButton();
			tag_checkButtonList[tag_checkButtonList.Count - 1].Add(checkButton);
			hBox.Add(checkButton);
			if (manualShow)
			{
				checkButton.Show();
			}
		}

		private void Button_Clicked_DestroyRow(object sender, EventArgs e)
		{
			if (entry_hBoxList.Count > 0)
			{
				entry_hBoxList[entry_hBoxList.Count - 1].Destroy();
				entry_hBoxList.RemoveAt(entry_hBoxList.Count - 1);
				tag_entryList.RemoveAt(tag_entryList.Count - 1);
				tag_checkButtonList.RemoveAt(tag_checkButtonList.Count - 1);
			}
		}

		private void CheckButton_Clicked_SelectAll(object sender, EventArgs e, CheckButton activeCheckButton, int index)
		{
			foreach (List<CheckButton> checkButtonList in tag_checkButtonList)
			{
				if (activeCheckButton.Active)
				{
					checkButtonList[index].Active = true;
				}
				else
				{
					checkButtonList[index].Active = false;
				}
			}
		}

		private void Button_Clicked_Run(object sender, EventArgs e)
		{
			ConfigManager.DeserializeFromXml();
			textBuffer.Clear();
			string givenFile;
			if (fileChooserButton.Filename == null)
			{
				givenFile = "";
			}
			else
			{
				givenFile = fileChooserButton.Filename;
			}
			if (conversion_checkButton.Active)
			{
				givenFile = VLCHandler.Run(givenFile, RunClass.fixerForceOverwrite, RunClass.fixerKeepTemp, RunClass.deleteOriginal);
			}
			if (tagEdit_checkButton.Active)
			{
				List<bool> tagRemoveList = new List<bool>();
				List<bool> tagAddList = new List<bool>();
				List<string> tagNameList = new List<string>();
				List<string> tagValueList = new List<string>();
				for (int i = 0; i < tag_checkButtonList.Count; i++)
				{
					tagRemoveList.Add(tag_checkButtonList[i][0].Active);
					tagAddList.Add(tag_checkButtonList[i][1].Active);
					tagNameList.Add(tag_entryList[i][0].Text);
					tagValueList.Add(tag_entryList[i][1].Text);
				}
				TagHandler.removeList = tagRemoveList;
				TagHandler.addList = tagAddList;
				TagHandler.nameList = tagNameList;
				TagHandler.valueList = tagValueList;
				List<string> outputList = TagHandler.Run(givenFile, newName_entry.Text);
				if (outputList.Count > 0)
				{
					textBuffer.Text = String.Join("\n", outputList);
				}
			}
			Console.WriteLine("Run complete!");
		}

		private void ReadTags()
		{
		}
	}
}
