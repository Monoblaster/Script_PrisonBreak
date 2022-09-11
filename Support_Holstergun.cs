function Player::GunImages_Update(%player)
{
	if(!%player.client.hasSpawnedOnce)
	{
		return;
	}

	%gunMount = %player.GetMountNodeObject(7);
	if(!%gunMount)
	{
		%gunMount = new AiPlayer()
		{
			dataBlock = GunBackPlayer;
			isGunImages = true;
		};
		%player.mountObject(%gunMount,7);
		%gunMount.setnetflag(6,true);
		%gunMount.setScopeAlways();
		%gunMount.applyDamage(10000);

		%gunMount.GunImages_CheckTPS(%player);
	}

	%pDB = %player.getDataBlock();
	%count = %pDB.maxTools;
	for(%i = 0; %i < %count; %i++)
	{
		if(%player.CurrTool == %i)
		{
			continue;
		}

		%iDB = %player.tool[%i];
		%itemTags = %iDB.ItemSlots_Tags;
		%itemTagCount = getWordCount(%itemTags);
		for(%j = 0; %j < %itemTagCount; %j++)
		{
		 	%tag = getWord(%itemTags,%j);
			if(%tag $= "Primary")
			{
				%backImage = %iDB.image.getId();
				break;
			}
		}
	}

	for(%i = 0; %i < %count; %i++)
	{
		if(%player.CurrTool == %i)
		{
			continue;
		}

		%iDB = %player.tool[%i];
		%itemTags = %iDB.ItemSlots_Tags;
		%itemTagCount = getWordCount(%itemTags);
		for(%j = 0; %j < %itemTagCount; %j++)
		{
		 	%tag = getWord(%itemTags,%j);
			if(%tag $= "Secondary")
			{
				%sideImage = %iDB.image.getId();
				break;
			}
		}
	}

	if(!%backImage)
	{
		%gunMount.UnmountImage(0);
	}
	else
	{
		%gunMount.mountImage(%backImage,0);
	}	

	if(!%sideImage)
	{
		%gunMount.UnmountImage(1);
	}
	else
	{
		%gunMount.mountImage(%sideImage.getName() @ "GunImagesSIDE",1);
	}
}

function GunBackPlayer::DoDismount(%db,%obj)
{
	return "";
}

function AiPlayer::GunImages_CheckTPS(%gunMount,%player)
{
	if(!isObject(%player.client))
	{
		return;
	}

	if(%player.isFirstPerson() && !%player.GunImages_isFirstPerson)
	{
		%gunMount.clearScopeToClient(%player.client);
		%player.GunImages_isFirstPerson = true;
	}

	if(!%player.isFirstPerson() && %player.GunImages_isFirstPerson)
	{
		%gunMount.ScopeToClient(%player.client);
		%player.GunImages_isFirstPerson = false;
	}
	
	%gunMount.schedule(33,"GunImages_CheckTPS",%player);
}

function GunImages_GenerateSideImages()
{
	%group = DatablockGroup;
	%count = %group.getCount();
	for(%i = 0; %i < %count; %i++)
	{
		%db = %group.getObject(%i);
		%itemTags = %db.ItemSlots_Tags;
		if(%itemTags $= "")
		{
			continue;
		}
		%itemTagCount = getWordCount(%itemTags);
		for(%j = 0; %j < %itemTagCount; %j++)
		{
		 	%tag = getWord(%itemTags,%j);
			if(%tag $= "Secondary")
			{
				%eval = %eval @ "datablock ShapeBaseImageData(" @ %db.image.getName() @ "GunImagesSIDE)";
				%eval = %eval @ "{";
					%eval = %eval @ "shapeFile = \"" @ %db.image.shapeFile @ "\";";
					%eval = %eval @ "doColorShift = " @ (%db.image.doColorShift && 1) @ ";";
					%eval = %eval @ "colorShiftColor = \"" @ %db.image.colorShiftColor @ "\";";
					%eval = %eval @ "mountPoint = 1;";
				%eval = %eval @ "};";
				eval(%eval);
			}
		}
	}
}

package GunImages
{
	function ServerCmdUseTool (%client, %slot)
	{
		%r = parent::ServerCmdUseTool(%client, %slot);
		%player = %client.player;
		if(isObject(%player))
		{
			%player.GunImages_Update();
		}
		return %r;
	}

	function ServerCmdUnUseTool (%client)
	{
		%r = parent::ServerCmdUnUseTool (%client);
		%player = %client.player;
		if(isObject(%player))
		{
			%player.GunImages_Update();
		}
		return %r;
	}

	function ServerCmdDropTool (%client, %position)
	{
		%r = parent::ServerCmdDropTool (%client, %position);
		%player = %client.player;
		if(isObject(%player))
		{
			%player.GunImages_Update();
		}
		return %r;
	}

	function ShapeBase::pickup (%this, %obj, %amount)
	{
		%r = parent::pickup (%this, %obj, %amount);

		%this.GunImages_Update();
		return %r;
	}

	function Armor::OnAdd(%db,%obj)
	{
		%r = parent::OnAdd(%db,%obj);
		%obj.schedule(1000,"GunImages_Update");
		return %r;
	}

	function Armor::OnDisabled(%db,%obj)
	{
		if(%obj.GetMountNodeObject(7))
		{
			%obj.GetMountNodeObject(7).delete();
		}
		return parent::OnDisabled(%db,%obj);
	}

	function ShapeBaseData::onUnmount(%DB,%Obj,%mount,%node)
	{
		if(%obj.isGunImages)
		{
			%obj.delete();
		}
		return Parent::onUnmount(%DB,%Obj,%mount,%node);
	}

	function GameConnection::onClientEnterGame (%client)
	{
		%r = parent::onClientEnterGame (%client);
		%group = ClientGroup;
		%count = %group.getCount();
		for(%i = 0; %i < %count; %i++)
		{
			%currplayer = %group.getObject(%i).player;
			if(isObject(%currplayer))
			{
				%gunMount = %currplayer.getMountNodeObject(7);
				%currPlayer.unMountObject(%gunMount);
				%currPlayer.GunImages_Update();
				%currPlayer.GunImages_isFirstPerson = false;
			}
		}
		return %r;
	}
};
activatePackage("GunImages");

GunImages_GenerateSideImages();