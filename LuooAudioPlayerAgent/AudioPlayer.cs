using System;
using System.Diagnostics;
using System.Windows;
using Microsoft.Phone.BackgroundAudio;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Net;
using System.IO;
namespace LuooAudioPlayerAgent
{
	public class AudioPlayer : AudioPlayerAgent
	{
		#region 字段

		static WebClient getMaxAlbumWebClient=new WebClient();
		static WebClient getListWebClient=new WebClient();
		static int currentTrackNumber=0;
		static int maxAlbumNumber=-1;
		static int currentAlbumNumber=-1;
		static ManualResetEvent manulResetEvent=new ManualResetEvent(false);
		private static List<AudioTrack> playList=new List<AudioTrack>();

		#endregion

		#region Constructor and error code
		static AudioPlayer()
		{
			getMaxAlbumWebClient.OpenReadCompleted+=GetMaxAlbumNumber_OpenReadCompleted;
			getListWebClient.OpenReadCompleted+=GetListWebClient_OpenReadCompleted;
			Deployment.Current.Dispatcher.BeginInvoke(delegate
			{
				Application.Current.UnhandledException += UnhandledException;
			});
		}

		#region 出现未处理的异常时执行的代码
		private static void UnhandledException( object sender , ApplicationUnhandledExceptionEventArgs e )
		{
			if(Debugger.IsAttached)
			{
				// 出现未处理的异常；强行进入调试器
				Debugger.Break();
			}
		}
		#endregion
		#endregion

		#region override AudioPlayerAgent

		protected override void OnPlayStateChanged( BackgroundAudioPlayer player , AudioTrack track , PlayState playState )
		{
			switch(playState)
			{
				case PlayState.TrackEnded:
					PlayNextTrack(player);
					break;
				case PlayState.TrackReady:
					player.Play();
					break;

			}
			NotifyComplete();
		}
		protected override void OnUserAction( BackgroundAudioPlayer player , AudioTrack track , UserAction action , object param )
		{
			switch(action)
			{
				case UserAction.Play:
					if(maxAlbumNumber==-1)
					{
						try
						{
							playList.Clear();
							GetMaxAlbumNumber();
							GetList();
						}
						catch
						{
							playList.Clear();
							//todo..
							playList.Add(new AudioTrack(new Uri("http://www.baidu.com") , "error" , null , null , null));
						}
						player.Track=playList[currentTrackNumber];
					}
					if(player.Track!=null) player.Play();
					break;

				case UserAction.Pause:
					player.Pause();
					break;

				case UserAction.SkipNext:
					PlayNextTrack(player);
					break;

				case UserAction.SkipPrevious:
					PlayPreviousTrack(player);
					break;
				case UserAction.FastForward:
					if(currentAlbumNumber<maxAlbumNumber)
					{
						try
						{
							++currentAlbumNumber;
							GetList();
						}
						catch
						{
							--currentAlbumNumber;
							//todo...
							playList.Add(new AudioTrack(new Uri("http://www.baidu.com") , "error" , null , null , null));
						}
						player.Track=playList[currentTrackNumber];
					}
					break;
				case UserAction.Rewind:
					try
					{
						--currentAlbumNumber;
						GetList();
					}
					catch
					{
						++currentAlbumNumber;
						//todo..
						playList.Add(new AudioTrack(new Uri("http://www.baidu.com") , "error" , null , null , null));
					}
					player.Track=playList[currentTrackNumber];
					break;
				case UserAction.Stop:
					break;
			}
			NotifyComplete();
		}
		protected override void OnError( BackgroundAudioPlayer player , AudioTrack track , Exception error , bool isFatal )
		{
			if(isFatal)
			{
				Abort();
			}
			else
			{
				NotifyComplete();
			}

		}
		protected override void OnCancel()
		{
		}

		#endregion

		#region NextTrack And PreviousTrack

		private void PlayNextTrack( BackgroundAudioPlayer player )
		{
			if(++currentTrackNumber >=playList.Count)
			{
				currentTrackNumber = 0;
			}
			player.Track=playList[currentTrackNumber];
		}

		private void PlayPreviousTrack( BackgroundAudioPlayer player )
		{

			if(--currentTrackNumber < 0)
			{
				currentTrackNumber = playList.Count - 1;
			}
			player.Track=playList[currentTrackNumber];

		}

		#endregion

		#region 获取当前期数新播放列表
		public static void GetList()
		{
			playList.Clear();
			if(maxAlbumNumber!=-1)
			{
				getListWebClient.OpenReadAsync(new Uri("http://www.luoo.net/music/"+currentAlbumNumber));
				manulResetEvent.Reset();
				manulResetEvent.WaitOne();
			}
			else throw new WebException();
		}
		static void GetListWebClient_OpenReadCompleted( object sender , OpenReadCompletedEventArgs e )
		{
			try
			{
				StreamReader sR=new StreamReader(e.Result);
				string html=sR.ReadToEnd();
				int playListCount;
				string pattern;
				MatchCollection matches;
				AudioTrack t;

				#region getAlbumAndArtist
				pattern=@"track-album.>(.+)-(.+)</a>";
				matches=Regex.Matches(@html , pattern);
				playListCount=matches.Count;
				for(int i=0 ; i<playListCount ; i++)
				{
					t=new AudioTrack();
					t.BeginEdit();
					t.Album=matches[i].Groups[1].ToString();
					t.Artist=matches[i].Groups[2].ToString();
					t.EndEdit();
					playList.Add(t);
				}
				#endregion

				#region getList
				pattern=@"http\S+01.mp3";
				Match matche=Regex.Match(html , pattern);
				string title=matche.ToString().Replace(@"\" , null);
				title=title.Remove(title.Length-6);
				for(int i=0 ; i<playListCount ; i++)
				{
					playList[i].BeginEdit();
					if(i+1<10)
						playList[i].Source=new Uri(title+"0"+(i+1).ToString()+".mp3");
					else
						playList[i].Source=new Uri(title+(i+1).ToString()+".mp3");
					playList[i].EndEdit();
				}
				#endregion

				#region getMusicTitleAndMusicCover
				pattern=@"img alt=.(.+). src=.(http://img.luoo.net/\S+60x60.jpg)";
				matches=Regex.Matches(@html , pattern);
				for(int i=0 ; i<playListCount ; i++)
				{
					t=playList[i];
					t.BeginEdit();
					t.Title=matches[i].Groups[1].ToString();
					t.Tag=currentAlbumNumber+"|"+matches[i].Groups[2].ToString();
					t.EndEdit();
				}
				#endregion

				#region getAlbumCover
				pattern=@"http://img.luoo.net/\S+640x452.jpg";
				matches=Regex.Matches(@html , pattern);
				Uri cover=new Uri(matches[0].ToString() , UriKind.Absolute);
				for(int i=0 ; i<playListCount ; i++)
				{
					playList[i].BeginEdit();
					playList[i].AlbumArt=cover;
					playList[i].EndEdit();
				}
				#endregion

				#region getAlbumTitle
				pattern=@"<title>(\S+)</title>";
				matches=Regex.Matches(@html , pattern);
				string temp="|"+matches[0].Groups[1].ToString();
				for(int i=0 ; i<playListCount ; i++)
				{
					playList[i].BeginEdit();
					playList[i].Tag+=temp;
					playList[i].EndEdit();
				}
				#endregion

				//
				sR.Close();
			}
			catch
			{
				//throw new Exception();
				playList.Add(new AudioTrack(new Uri("http://www.baidu.com") , "error" , null , null , null));
			}
			currentTrackNumber=0;
			manulResetEvent.Set();
		}
		#endregion

		#region 获取当前最大的期数
		public static void GetMaxAlbumNumber()
		{
			getMaxAlbumWebClient.OpenReadAsync(new Uri("http://www.luoo.net"));
			//wait...
			manulResetEvent.WaitOne();
		}
		static void GetMaxAlbumNumber_OpenReadCompleted( object sender , OpenReadCompletedEventArgs e )
		{
			try
			{
				//if(e.Error!=null) throw e.Error;
				StreamReader sR=new StreamReader(e.Result);
				//close
				string html=sR.ReadToEnd();
				//GetMaxAlbumNumber
				string pattern=@"vol. [0-9]+";
				MatchCollection matches=Regex.Matches(@html , pattern);
				string temp=matches[0].ToString().Split(' ')[1];
				maxAlbumNumber=int.Parse(temp);
				//set currentAlbumNumber
				currentAlbumNumber=maxAlbumNumber;
				//
				sR.Close();
			}
			catch(Exception ex)
			{
				//throw ex;
			}
			manulResetEvent.Set();
		}
		#endregion


	}
}