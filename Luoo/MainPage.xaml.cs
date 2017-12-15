using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Luoo.Resources;
using System.Windows.Media.Imaging;
using Coding4Fun.Toolkit.Controls;
using Microsoft.Phone.BackgroundAudio;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using Microsoft.Phone.Net.NetworkInformation;

namespace Luoo
{
	public partial class MainPage : PhoneApplicationPage
	{
		#region 字段
		static bool isAlbumChanged=true;
		static int currentAlbumNumber=-1;
		static int maxAlbumNumber=-1;
		static bool isExit=false;
		#endregion

		#region 构造函数
		public MainPage()
		{
			isExit=false;

			InitializeComponent();
			BuildLocalizedApplicationBar();

			BackgroundAudioPlayer.Instance.PlayStateChanged+=Instance_PlayStateChanged;

			SystemTray.SetIsVisible(this , true);
			SystemTray.SetOpacity(this , 0);


		}
		#endregion

		#region 应用程序栏

		private void BuildLocalizedApplicationBar()
		{
			ApplicationBar = new ApplicationBar();
			ApplicationBar.Mode=ApplicationBarMode.Minimized;
			ApplicationBar.IsVisible=true;

			ApplicationBarMenuItem callItem = new ApplicationBarMenuItem(AppResources.CallLuoo);
			callItem.Click+=callItem_Click;

			ApplicationBarMenuItem aboutItem=new ApplicationBarMenuItem(AppResources.About);
			aboutItem.Click+=aboutItem_Click;

			ApplicationBarMenuItem changeBackgroundItem=new ApplicationBarMenuItem(AppResources.ChangeBackground);
			changeBackgroundItem.Click+=changeBackgroundItem_Click;

			ApplicationBar.MenuItems.Add(callItem);
			ApplicationBar.MenuItems.Add(changeBackgroundItem);
			ApplicationBar.MenuItems.Add(aboutItem);
		}
		void changeBackgroundItem_Click( object sender , EventArgs e )
		{
			BitmapImage bC=backgroundImage.Source as BitmapImage;
			if(bC.UriSource.ToString().Equals("assets/Tiles/background3.png"))
			{
				backgroundImage.Source=new BitmapImage(new Uri("assets/Tiles/background4.png" , UriKind.Relative));
			}
			else backgroundImage.Source=new BitmapImage(new Uri("assets/Tiles/background3.png" , UriKind.Relative));
		}

		private void aboutItem_Click( object sender , EventArgs e )
		{
			AboutPrompt aboutPrompt=new AboutPrompt();
			aboutPrompt.VersionNumber="version:1.0";
			aboutPrompt.Title="关于Luoo";
			aboutPrompt.Show("XiaoTian" , "" , "tian_feng_bo@live.com" , @"http://www.cnblogs.com/au-xiaotian/");
		}

		void callItem_Click( object sender , EventArgs e )
		{
			Windows.System.Launcher.LaunchUriAsync(new System.Uri("http://www.luoo.net"));
		}

		#endregion

		#region 播放控制

		private void playAndPauseButton_Click( object sender , RoutedEventArgs e )
		{
			playAndPauseStoryBoard.Begin();
			UnableAllButton();
			BitmapImage bI=playAndPause.Source as BitmapImage;
			if(bI.UriSource.ToString().Equals("/assets/icons/play.png"))
			{
				playAndPause.Source=new BitmapImage(new Uri("/assets/icons/pause.png" , UriKind.Relative));
				BackgroundAudioPlayer.Instance.Play();
			}
			else
			{
				playAndPause.Source=new BitmapImage(new Uri("/assets/icons/play.png" , UriKind.Relative));
				BackgroundAudioPlayer.Instance.Pause();
			}
			EnableAllButton();

		}

		private void lastAlbumButton_Click( object sender , RoutedEventArgs e )
		{
			lastAlbumStoryBoard.Begin();
			progressBar.Visibility=Visibility.Visible;
			UnableAllButton();
			BackgroundAudioPlayer.Instance.Rewind();
			isAlbumChanged=true;
		}

		private void nextAlbumButton_Click( object sender , RoutedEventArgs e )
		{
			//判断是否有下一期
			if(currentAlbumNumber<maxAlbumNumber)
			{
				nextAlbumStoryBoard.Begin();
				progressBar.Visibility=Visibility.Visible;
				UnableAllButton();
				BackgroundAudioPlayer.Instance.FastForward();
				isAlbumChanged=true;
			}
		}

		private void nextSongButton_Click( object sender , RoutedEventArgs e )
		{
			nextSongStoryBoard.Begin();
			progressBar.Visibility=Visibility.Visible;
			UnableAllButton();
			BackgroundAudioPlayer.Instance.SkipNext();
		}

		private void lastSongButton_Click( object sender , RoutedEventArgs e )
		{
			lastSongStoryBoard.Begin();
			progressBar.Visibility=Visibility.Visible;
			UnableAllButton();
			BackgroundAudioPlayer.Instance.SkipPrevious();
		}
		#endregion

		#region UI Control
		private void UpdateUI()
		{
			if(BackgroundAudioPlayer.Instance.PlayerState==PlayState.Playing)
			{
				playAndPause.Source=new BitmapImage(new Uri("/assets/icons/pause.png" , UriKind.Relative));
			}
			else
			{
				playAndPause.Source=new BitmapImage(new Uri("/assets/icons/play.png" , UriKind.Relative));
			}

			var t=BackgroundAudioPlayer.Instance.Track;
			if(isAlbumChanged)
			{
				//set albumCover
				albumCover.Opacity=0;
				albumCover.ImageSource=new BitmapImage(t.AlbumArt);
				//图像淡入
				albumCover.ImageOpened+=albumCover_ImageOpened;
			}
			//set album
			album.Text=t.Album;
			//set artist
			artist.Text=t.Artist;
			//set musicTitle
			title.Text=t.Title;
			//set AlbumTitleAndMusicCover
			string[] ar=t.Tag.Split('|');
			musicCover.Opacity=0;
			musicCover.ImageSource=new BitmapImage(new Uri(ar[1] , UriKind.Absolute));
			//图像淡入
			musicCover.ImageOpened+=musicCover_ImageOpened;
			//set topic
			topic.Text="Vol."+ar[0]+" "+ar[2];
			//set currentAlbumNumber
			if(maxAlbumNumber==-1) maxAlbumNumber=int.Parse(ar[0]);
			currentAlbumNumber=int.Parse(ar[0]);
		}

		private void UnableAllButton()
		{
			playAndPauseButton.IsEnabled=false;
			lastAlbumButton.IsEnabled=false;
			nextAlbumButton.IsEnabled=false;
			nextSongButton.IsEnabled=false;
			lastSongButton.IsEnabled=false;
		}

		private void EnableAllButton()
		{
			playAndPauseButton.IsEnabled=true;
			lastAlbumButton.IsEnabled=true;
			nextAlbumButton.IsEnabled=true;
			nextSongButton.IsEnabled=true;
			lastSongButton.IsEnabled=true;
		}

		void albumCover_ImageOpened( object sender , RoutedEventArgs e )
		{
			albumCoverStoryBoard.Begin();
			albumCover.Opacity=0.8;
		}

		void musicCover_ImageOpened( object sender , RoutedEventArgs e )
		{
			musicCoverStoryBoard.Begin();
			musicCover.Opacity=0.8;
		}

		void Instance_PlayStateChanged( object sender , EventArgs e )
		{
			bool t=false;
			t=BackgroundAudioPlayer.Instance.Track.Title.Equals("error");
			if(!t)
			{
				var state=e as PlayStateChangedEventArgs;
				if(state.CurrentPlayState==PlayState.Playing)
				{
					playAndPause.Source=new BitmapImage(new Uri("/assets/icons/pause.png" , UriKind.Relative));
				}
				else
				{
					playAndPause.Source=new BitmapImage(new Uri("/assets/icons/play.png" , UriKind.Relative));
				}
				UpdateUI();
				EnableAllButton();
				progressBar.Visibility=Visibility.Collapsed;
				isAlbumChanged=false;
			}
			else
			{
				progressBar.Visibility=Visibility.Collapsed;
				BackgroundAudioPlayer.Instance.Close();
				ToastPrompt toast=new ToastPrompt();
				toast.Message="网络不可用，请检查网络连接";
				toast.Show();
			}
		}
		#endregion

		#region Override Methods
		protected override void OnNavigatedTo( NavigationEventArgs e )
		{
			ToastPrompt toast=new ToastPrompt();
			if(DeviceNetworkInformation.IsNetworkAvailable)
			{
				if(BackgroundAudioPlayer.Instance.Track==null)
				{
					progressBar.Visibility=Visibility.Visible;
					UnableAllButton();
					BackgroundAudioPlayer.Instance.Play();
				}
				else
				{
					UpdateUI();
				}
			}
			else
			{
				toast.Message="当前网络不可用";
				toast.Show();
			}
			base.OnNavigatedTo(e);
		}

		protected override void OnBackKeyPress( System.ComponentModel.CancelEventArgs e )
		{
			if(!isExit)
			{
				isExit=true;
				textStoryBoard.Begin();
				Task.Factory.StartNew(() => {
					Thread.Sleep(2000);
					this.Dispatcher.BeginInvoke(() => isExit=false);
				});
				e.Cancel=true;
			}
			else
			{
				MessageBoxResult result=MessageBox.Show("停止播放音乐？" , "退出" , MessageBoxButton.OKCancel);
				if(result==MessageBoxResult.OK)
				{
					BackgroundAudioPlayer.Instance.Close();
				}
				e.Cancel=false;
			}
		}
		#endregion

	}
}