#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;
using OpenRA.Widgets;
using OpenRA.Traits.Activities;
using OpenRA.FileFormats;
using OpenRA.Mods.RA.Activities;

namespace OpenRA.Mods.RA
{
	class Gdi01ScriptInfo : TraitInfo<Gdi01Script> { }

	class Gdi01Script: ILoadWorldHook, ITick
	{		
		Dictionary<string, Actor> Actors;
		Dictionary<string, Player> Players;
		Map Map;
		public void WorldLoaded(World w)
		{
			Map = w.Map;
			Players = w.WorldActor.Trait<CreateMapPlayers>().Players;
			Actors = w.WorldActor.Trait<SpawnMapActors>().Actors;		
			Game.MoveViewport((.5f * (w.Map.TopLeft + w.Map.BottomRight).ToFloat2()).ToInt2());
			
			var playerRoot = Widget.RootWidget.OpenWindow("FMVPLAYER");
			var player = playerRoot.GetWidget<VqaPlayerWidget>("PLAYER");
			w.DisableTick = true;
			
			player.Load("gdi1.vqa");
			player.PlayThen(() =>
			{
				player.Load("landing.vqa");
				player.PlayThen(() =>
				{
					Widget.RootWidget.CloseWindow();
					w.DisableTick = false;
					Sound.PlayMusic(Rules.Music["aoi"].Filename);
					started = true;
				});
			});
		}
		
		int ticks = 0;
		bool started = false;
		public void Tick(Actor self)
		{
			if (!started)
				return;
			
			if (ticks == 0)
				SetGunboatPath();
			
			
			if (ticks == 25*5)
			{
				ReinforceFromSea(self.World, 
				                 Map.Waypoints["lstStart"],
				                 Map.Waypoints["lstEnd"],
				                 new int2(53,53),
				                 new string[] {"e1","e1","e1"});
			}
			
			if (ticks == 25*15)
			{
				ReinforceFromSea(self.World, 
				                 Map.Waypoints["lstStart"],
				                 Map.Waypoints["lstEnd"],
				                 new int2(53,53),
				                 new string[] {"e1","e1","e1"});
			}
			
			if (ticks == 25*30)
			{
				ReinforceFromSea(self.World, 
				                 Map.Waypoints["lstStart"],
				                 Map.Waypoints["lstEnd"],
				                 new int2(53,53),
				                 new string[] {"jeep"});
			}
			
			if (ticks == 25*60)
			{
				ReinforceFromSea(self.World, 
				                 Map.Waypoints["lstStart"],
				                 Map.Waypoints["lstEnd"],
				                 new int2(53,53),
				                 new string[] {"jeep"});
			}
			
			ticks++;
		}
		
		void SetGunboatPath()
		{
			Actors["Gunboat"].QueueActivity(new Move( Map.Waypoints["gunboatRight"],1));
			Actors["Gunboat"].QueueActivity(new Move( Map.Waypoints["gunboatLeft"],1));
			Actors["Gunboat"].QueueActivity(new CallFunc(() => SetGunboatPath()));
		}
		
		void ReinforceFromSea(World world, int2 startPos, int2 endPos, int2 unload, string[] items)
		{
			world.AddFrameEndTask(w =>
			{
				Sound.PlayToPlayer(w.LocalPlayer,"reinfor1.aud");				

				var a = w.CreateActor("lst", new TypeDictionary 
				{
					new LocationInit( startPos ),
					new OwnerInit( Players["GoodGuy"] ),
					new FacingInit( 0 ),
				});
				
				var cargo = a.Trait<Cargo>();
				foreach (var i in items)
					cargo.Load(a, world.CreateActor(false, i.ToLowerInvariant(), new TypeDictionary { new OwnerInit( Players["GoodGuy"] ) }));
				
				a.CancelActivity();
				a.QueueActivity(new Move(endPos, 0));
				a.QueueActivity(new CallFunc(() =>
				{
					while (!cargo.IsEmpty(a))
					{
						var b = cargo.Unload(a);
						world.AddFrameEndTask(w2 =>
						{
							w2.Add(b);
							b.TraitsImplementing<IMove>().FirstOrDefault().SetPosition(b, a.Location);
							b.QueueActivity(new Move(unload, 2));
						});
					}
				}));
				a.QueueActivity(new Wait(25));
				a.QueueActivity(new Move(startPos,0));
				a.QueueActivity(new RemoveSelf());
			});
		}
	}
}