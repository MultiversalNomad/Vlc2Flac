using Gtk;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Vlc2Flac
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			args = new string[]{"-gui", "-c"};//, "-ucl"};
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
				CustomMainWindow myWin = new CustomMainWindow("Hello! 今日は！", !useCustomLoop);

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
				Console.WriteLine(myWin.entry.Text);
				myWin.Destroy();
				//GLib.Log.DefaultHandler("", GLib.LogLevelFlags.Message, entry.Text);
				//GLib.Log.PrintLogFunction("", GLib.LogLevelFlags.Message, entry.Text);
				//GLib.Log.PrintTraceLogFunction("", GLib.LogLevelFlags.Message, entry.Text);

				if (!showConsole)
				{
					ShowWindow(h, 1);
				}
			}
			Console.WriteLine("Vlc2Flac DONE!");
			Console.Write("Press any key to close... ");
			Console.ReadKey();
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
			exeFlacFixer = new FileInfo(@"E:\JPLC Data\Projects\C#\FlacFixer\FlacFixer\bin\Debug\FlacFixer.exe");
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
					startInfo.Arguments = String.Concat("/C \"\"", @"C:\Program Files (x86)\VideoLAN\VLC\vlc.exe", "\" -I dummy -vvv \"", initialFile.FullName, "\" --no-sout-video --sout-audio --no-sout-rtp-sap --no-sout-standard-sap --ttl=1 --sout-keep --sout=#transcode{acodec=flac}:std{mux=raw,dst=\"", outputTempFile.FullName, "\"} vlc://quit\"");
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
					//
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
					startInfo.Arguments = String.Concat("/C \"\"", @"E:\JPLC Data\Projects\C#\FlacFixer\FlacFixer\bin\Debug\FlacFixer.exe", "\" \"", outputFile.FullName, "\"",
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
					//Console.WriteLine("outputList start");
					foreach (string line in outputList)
					{
						//Console.WriteLine(line);
						if (line != null)
						{
							string trimmedLine = line.Trim();
							if (trimmedLine.StartsWith(checkString))
							{
								finalFilePath = trimmedLine.Substring(checkString.Length);
							}
						}
					}
					//Console.WriteLine("outputList end");
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
					string initialCommand = String.Concat("/C \"\"", @"C:\Program Files (x86)\FLAC Frontend\tools\metaflac.exe", "\" \"", editFile.FullName, "\" --no-utf8-convert");
					string command = initialCommand;
					Console.WriteLine(nameList.Count);
					for (int i = 0; i < nameList.Count; i++)
					{
						/*
						Console.WriteLine(removeList[i]);
						Console.WriteLine(nameList[i]);
						Console.WriteLine(addList[i]);
						Console.WriteLine(valueList[i]);
						*/
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
					Console.WriteLine(initialCommand);
					Console.WriteLine(command);
					//
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
					//string[] tempArray = File.ReadAllLines(@"C:\Users\JPLC\Videos\RealPlayer Downloads\outputTest.txt");
					//outputList = new List<string>(tempArray);
					outputList = new List<string>(tempOutput.Split('\n'));
					Console.WriteLine(outputList.Count);
					foreach (string line in outputList)
					{
						Console.WriteLine(line);
					}
					//
					Console.WriteLine("EDIT FILE:");
					Console.WriteLine(editFile.FullName);
					if ((newName != null) && (newName != ""))
					{
						newName = String.Concat(newName, ".flac");
						if (newName != editFile.Name)
						{
							string newFilePath = String.Concat(editFile.DirectoryName, @"\", newName);
							File.Move(editFile.FullName, newFilePath);
							Console.WriteLine("FINAL FILE:");
							Console.WriteLine(newFilePath);
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
			//eventBox.ButtonReleaseEvent += new ButtonReleaseEventHandler((curSender, curE) => Widget_ButtonRelease(curSender, curE, eventBox));
			Add(eventBox);
			//
			//Layout layout = new Layout(new Adjustment(0d, 0d, 10d, 1d, 5d, 1000d), new Adjustment(0d, 0d, 0d, 0d, 0d, 0d));
			//Layout layout = new Layout(null, null);
			//eventBox.Add(layout);
			//
			vBox = new VBox();
			//layout.Add(vBox);
			eventBox.Add(vBox);
			//
			label = new Label();
			label.Text = "Nice to meet you. 始めまして。";
			vBox.Add(label);
			//
			fileChooserButton = new FileChooserButton("wheeeee", FileChooserAction.Open);
			//fileChooserButton.SelectMultiple = true; // Multiple files not supported.
			vBox.Add(fileChooserButton);
			/*
			FileChooserDialog fileChooserDialog = new FileChooserDialog("whee", null, FileChooserAction.Open, "OK", ResponseType.Ok);
			fileChooserDialog.SelectMultiple = true;
			int response = fileChooserDialog.Run();
			if ((ResponseType)response == ResponseType.Ok)
			{
				string[] tempArray = fileChooserDialog.Filenames;
				foreach (string tempElement in tempArray)
				{
					Console.WriteLine(tempElement);
				}
			}
			fileChooserDialog.Destroy();
			*/
			/*
			FileChooserWidget fileChooserWidget = new FileChooserWidget(FileChooserAction.Open);
			fileChooserWidget.SelectMultiple = true;
			//fileChooserWidget.ButtonReleaseEvent += new ButtonReleaseEventHandler((curSender, curE) => Widget_ButtonRelease(curSender, curE, fileChooserWidget));
			vBox.Add(fileChooserWidget);
			*/
			/*			
			Widget[] tempArray = vBox.FocusChain;
			Console.WriteLine("Focus Chain Start");
			foreach (Widget tempElement in tempArray)
			{
				Console.WriteLine(tempElement.Name);
			}
			Console.WriteLine("Focus Chain End");
			*/
			//
			label = new Label();
			label.Text = "Whee";
			vBox.Add(label);
			//
			hBox = new HBox();
			vBox.Add(hBox);
			//
			tagRemoveAll_checkButton = new CheckButton();
			tagRemoveAll_checkButton.Label = "Select all for removal";
			tagRemoveAll_checkButton.Clicked += new EventHandler((curSender, curE) => CheckButton_Clicked_SelectAll(curSender, curE, tagRemoveAll_checkButton, 0));
			hBox.Add(tagRemoveAll_checkButton);
			//
			label = new Label();
			label.Text = "Tag name";
			hBox.Add(label);
			//
			label = new Label();
			label.Text = "Tag value";
			hBox.Add(label);
			//
			tagAddAll_checkButton = new CheckButton();
			tagAddAll_checkButton.Label = "Select all for addition";
			tagAddAll_checkButton.Clicked += new EventHandler((curSender, curE) => CheckButton_Clicked_SelectAll(curSender, curE, tagAddAll_checkButton, 1));
			hBox.Add(tagAddAll_checkButton);
			//
			tag_vBox = new VBox();
			vBox.Add(tag_vBox);
			//
			List<string> tagTitleList = new List<string>(){"TITLE", "ARTIST", "ALBUMARTIST", "ALBUM", "TRACKNUMBER"};
			foreach (string tagTitle in tagTitleList)
			{
				AddTagEntryRow(false, tagTitle);
			}
			//
			foreach (List<Entry> entryList in tag_entryList)
			{
				//Console.WriteLine(entryList.Count);
				Console.WriteLine(String.Concat(entryList[0].Text, "=", entryList[1].Text));
			}
			//
			hBox = new HBox();
			button_hBox = hBox;
			vBox.Add(hBox);
			//
			button = new Button();
			button.Label = "Add Fields";
			button.Clicked += new EventHandler(Button_Clicked_AddRow);
			hBox.Add(button);
			//
			button = new Button();
			button.Label = "Remove Fields";
			button.Clicked += new EventHandler(Button_Clicked_DestroyRow);
			hBox.Add(button);
			//
			conversion_checkButton = new CheckButton();
			conversion_checkButton.Label = "Convert from vlc.exe-compatible file to FLAC file";
			vBox.Add(conversion_checkButton);
			//
			tagEdit_checkButton = new CheckButton();
			tagEdit_checkButton.Label = "Edit tags with metaflac.exe (tag removal first, tag addition second)";
			vBox.Add(tagEdit_checkButton);
			//
			newName_entry = new Entry();
			vBox.Add(newName_entry);
			//
			button = new Button();
			button.Label = "Run";
			button.Clicked += new EventHandler(Button_Clicked_Run);
			vBox.Add(button);
			//
			label = new Label();
			label.Text = "Output Information";
			vBox.Add(label);
			//
			textTagTable = new TextTagTable();
			//
			textBuffer = new TextBuffer(textTagTable);
			//
			textView = new TextView();
			textView.Buffer = textBuffer;
			textView.Editable = false;
			vBox.Add(textView);
		}

		private void Button_Clicked_AddRow(object sender, EventArgs e)
		{
			Console.WriteLine("Add Row");
			//vBox.Remove(button_hBox);
			//
			AddTagEntryRow(true);
			//
			//vBox.Add(button_hBox);
			foreach (List<Entry> entryList in tag_entryList)
			{
				//Console.WriteLine(entryList.Count);
				Console.WriteLine(String.Concat(entryList[0].Text, "=", entryList[1].Text));
			}
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
			//
			tag_checkButtonList.Add(new List<CheckButton>());
			AddTagCheckButton(manualShow);
			//
			List<string> entryTextList = new List<string>(){tagName, tagValue};
			tag_entryList.Add(new List<Entry>());
			foreach (string entryText in entryTextList)
			{
				AddTagEntry(manualShow, entryText);
			}
			//
			AddTagCheckButton(manualShow);
			//
		}

		private void AddTagEntry(bool manualShow = false, string tagText = "")
		{
			entry = new Entry();
			entry.Text = tagText;
			entry.KeyReleaseEvent += new KeyReleaseEventHandler((curSender, curE) => Entry_KeyRelease(curSender, curE, entry));
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
			Console.WriteLine("Destroy Row");
			//vBox.Remove(button_hBox);
			if (entry_hBoxList.Count > 0)
			{
				entry_hBoxList[entry_hBoxList.Count - 1].Destroy();
				entry_hBoxList.RemoveAt(entry_hBoxList.Count - 1);
				tag_entryList.RemoveAt(tag_entryList.Count - 1);
				tag_checkButtonList.RemoveAt(tag_checkButtonList.Count - 1);
			}
			//vBox.Add(button_hBox);
			foreach (List<Entry> entryList in tag_entryList)
			{
				//Console.WriteLine(entryList.Count);
				Console.WriteLine(String.Concat(entryList[0].Text, "=", entryList[1].Text));
			}
		}

		private void Widget_ButtonRelease(object sender, ButtonReleaseEventArgs e, Widget activeWidget)
		{
			// Optional: Fix problem dealing with "stacked" calls. Not a huge issue; more cosmetic.
			Console.WriteLine(activeWidget.Name);
			if (e.Event.Button == 1)
			{
				Console.WriteLine("Left Click");
			}
			else if (e.Event.Button == 2)
			{
				Console.WriteLine("Middle Click");
			}
			else if (e.Event.Button == 3)
			{
				Console.WriteLine("Right Click");
			}
			activeWidget.HasFocus = true;
		}

		private void Entry_KeyRelease(object sender, KeyReleaseEventArgs e, Entry activeEntry)
		{
			if (e.Event.Key == Gdk.Key.Return)
			{
				Console.WriteLine("BOOP");
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
			textBuffer.Clear();
			Console.Write("FILE: ");
			string givenFile;
			if (fileChooserButton.Filename == null)
			{
				Console.Write("(NULL)");
				givenFile = "";
			}
			else
			{
				givenFile = fileChooserButton.Filename;
			}
			Console.WriteLine(givenFile);
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
				Console.WriteLine("TAG STUFF START");
				for (int i = 0; i < tag_checkButtonList.Count; i++)
				{
					tagRemoveList.Add(tag_checkButtonList[i][0].Active);
					tagAddList.Add(tag_checkButtonList[i][1].Active);
					tagNameList.Add(tag_entryList[i][0].Text);
					tagValueList.Add(tag_entryList[i][1].Text);

					Console.WriteLine(tagRemoveList[tagRemoveList.Count - 1]);
					Console.WriteLine(tagAddList[tagAddList.Count - 1]);
					Console.WriteLine(tagNameList[tagNameList.Count - 1]);
					Console.WriteLine(tagValueList[tagValueList.Count - 1]);
				}
				Console.WriteLine("TAG STUFF END");
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
