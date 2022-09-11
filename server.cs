exec("./itemInstance.cs");
$LimitedUseImage::Range = 5;

function LimitedUseImage::OnFire(%db,%player,%slot)
{
	%ii = %player.GetItemInstance(%player.currTool);
	%usesRemaining = %ii.get("usesRemaining");

	if(%usesRemaining $= "" || %usesRemaining == 0)
	{
		%ii.set("usesRemaining",%db.maxUses);
		%usesRemaining = %db.maxUses;
	}

	if(%usesRemaining > 0)
	{
		%start = %player.getEyeTransform();
		%look = %player.getMuzzleVector(%slot);

		%end = vectorAdd(%start,vectorScale(%look,$LimitedUseImage::Range));
		%mask = $TypeMasks::PlayerObjectType | $TypeMasks::VehicleObjectType | $TypeMasks::StaticObjectType | $TypeMasks::FxBrickObjectType;
		%hit = ContainerRayCast(%start,%end,%mask,%player);
		%obj = getWord(%hit,0);
		if(%obj)
		{
			if(isObject(%db.projectile))
			{
				new (%db.projectileType) ("")
				{
					dataBlock = %db.projectile;
					initialPosition = getWords(%hit,1);
					sourceObject = %player;
					sourceSlot = %slot;
					client = %player.client;
				}.explode();
			}
			
			if(%obj.getType() & $TypeMasks::FxBrickObjectType)
			{
				%check = getSafeVariableName(%player.tool[%player.currTool].uiname) @ "OnUse";
				%count = %obj.numEvents;
				for(%i = 0; %i < %count; %i++)
				{
					if(%obj.eventInput[%i] $= %check && %obj.eventEnabled[%i])
					{
						%db.OnUsed(%obj,%player,%slot);
						return %hit;
					}
				}
			}
		}	
	}

	return "";
}

function LimitedUseImage::OnUsed(%db,%brick,%player,%slot)
{
	%ii = %player.GetItemInstance(%player.currTool);
	%ii.add("usesRemaining",-1);

	eval("%brick." @ getSafeVariableName(%player.tool[%player.currTool].uiname) @ "OnUse(%player,%player.client);");
}

function LimitedUseImage_GenerateEvents()
{
	%group = DatablockGroup;
	%count = %group.getCount();
	for(%i = 0; %i < %count; %i++)
	{
		%currdb = %group.getObject(%i);
		if(%currdb.image.className $= "LimitedUseImage" && %currdb.uiname !$= "")
		{
			%eval = %eval @ "function fxDTSBrick::" @ getSafeVariableName(%currdb.uiname) @ "OnUse(%brick, %player, %client)";
			%eval = %eval @ "{";
				%eval = %eval @ "$InputTarget_[\"Self\"] = %brick;";
				%eval = %eval @ "$InputTarget_[\"Player\"] = %player;";
				%eval = %eval @ "$InputTarget_[\"Client\"] = %client;";
				%eval = %eval @ "$InputTarget_[\"Minigame\"] = 0;";
				%eval = %eval @ "if(getMiniGameFromObject (%brick) == getMiniGameFromObject (%client))";
				%eval = %eval @ "{";
					%eval = %eval @ "$InputTarget_[\"Minigame\"] = getMiniGameFromObject (%brick);";
				%eval = %eval @ "}";
				%eval = %eval @ "%brick.processInputEvent (" @ getSafeVariableName(%currdb.uiname) @ "OnUse, %client);";
			%eval = %eval @ "}";

			eval(%eval);
			registerInputEvent("fxDTSBrick", getSafeVariableName(%currdb.uiname) @ "OnUse", "Self fxDTSBrick" TAB "Player Player" TAB "Client GameConnection" TAB "MiniGame MiniGame");
		}
	}
}