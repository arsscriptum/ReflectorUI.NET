using System;
using System.Windows.Input;

namespace Reflector.UI
{
	public class AppCommands
	{
		public static RoutedCommand OpenAssemblyCommand;

		public static RoutedCommand CloseAssemblyCommand;

		public static RoutedCommand ExitCommand;

		public static RoutedCommand ShowAsmMgrCommand;

		public static RoutedCommand ShowBookmarksCommand;

		public static RoutedCommand ShowSearchCommand;

		public static RoutedCommand RefreshCommand;

		public static RoutedCommand OptionsCommand;

		public static RoutedCommand DisassembleCommand;

		public static RoutedCommand AnalyzeCommand;

		public static RoutedCommand ToggleBookmarkCommand;

		public static RoutedCommand LolCommand;

		static AppCommands()
		{
			Type type = typeof(AppCommands);
			InputGestureCollection inputGestureCollections = new InputGestureCollection();
			inputGestureCollections.Add(new KeyGesture(Key.O, ModifierKeys.Control));
			AppCommands.OpenAssemblyCommand = new RoutedCommand("App.OpenAssembly", type, inputGestureCollections);
			AppCommands.CloseAssemblyCommand = new RoutedCommand("App.CloseAssembly", typeof(AppCommands));
			Type type1 = typeof(AppCommands);
			InputGestureCollection inputGestureCollections1 = new InputGestureCollection();
			inputGestureCollections1.Add(new KeyGesture(Key.Q, ModifierKeys.Alt));
			AppCommands.ExitCommand = new RoutedCommand("App.Exit", type1, inputGestureCollections1);
			Type type2 = typeof(AppCommands);
			InputGestureCollection inputGestureCollections2 = new InputGestureCollection();
			inputGestureCollections2.Add(new KeyGesture(Key.A, ModifierKeys.Alt));
			AppCommands.ShowAsmMgrCommand = new RoutedCommand("App.ShowAsmMgr", type2, inputGestureCollections2);
			Type type3 = typeof(AppCommands);
			InputGestureCollection inputGestureCollections3 = new InputGestureCollection();
			inputGestureCollections3.Add(new KeyGesture(Key.F2));
			AppCommands.ShowBookmarksCommand = new RoutedCommand("App.ShowBookmarks", type3, inputGestureCollections3);
			Type type4 = typeof(AppCommands);
			InputGestureCollection inputGestureCollections4 = new InputGestureCollection();
			inputGestureCollections4.Add(new KeyGesture(Key.F3));
			AppCommands.ShowSearchCommand = new RoutedCommand("App.ShowSearch", type4, inputGestureCollections4);
			Type type5 = typeof(AppCommands);
			InputGestureCollection inputGestureCollections5 = new InputGestureCollection();
			inputGestureCollections5.Add(new KeyGesture(Key.F5));
			AppCommands.RefreshCommand = new RoutedCommand("App.Refresh", type5, inputGestureCollections5);
			AppCommands.OptionsCommand = new RoutedCommand("App.Options", typeof(AppCommands));
			Type type6 = typeof(AppCommands);
			InputGestureCollection inputGestureCollections6 = new InputGestureCollection();
			inputGestureCollections6.Add(new KeyGesture(Key.Space));
			AppCommands.DisassembleCommand = new RoutedCommand("App.Disassemble", type6, inputGestureCollections6);
			Type type7 = typeof(AppCommands);
			InputGestureCollection inputGestureCollections7 = new InputGestureCollection();
			inputGestureCollections7.Add(new KeyGesture(Key.R, ModifierKeys.Control));
			AppCommands.AnalyzeCommand = new RoutedCommand("App.Analyze", type7, inputGestureCollections7);
			Type type8 = typeof(AppCommands);
			InputGestureCollection inputGestureCollections8 = new InputGestureCollection();
			inputGestureCollections8.Add(new KeyGesture(Key.K, ModifierKeys.Control));
			AppCommands.ToggleBookmarkCommand = new RoutedCommand("App.ToggleBookmark", type8, inputGestureCollections8);
			Type type9 = typeof(AppCommands);
			InputGestureCollection inputGestureCollections9 = new InputGestureCollection();
			inputGestureCollections9.Add(new KeyGesture(Key.K, ModifierKeys.Alt));
			AppCommands.LolCommand = new RoutedCommand("App.LOL", type9, inputGestureCollections9);
		}

		public AppCommands()
		{
		}
	}
}