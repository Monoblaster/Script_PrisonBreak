package PBHud
{
	function GameConnection::onClientEnterGame(%client)
	{
		if(!isEventPending(%client.PBHud_Loop))
		{
			PBHud_Loop(%client);
		}

		return parent::onClientEnterGame(%client);
	}
};
activatePackage("PBHud");


function PBHud_Loop(%client)
{	
	PBHud_Update(%client);
	%client.PBHud_Loop = schedule(66, %client, PBHud_Loop, %client);
}

function PBHud_GenerateBar(%bar,%emptyColor)
{
	%b = ".";
	%len = 60;

	%fCount = getFieldCount(%bar);
	%total = 0;
	for(%i = 0; %i < %fCount; %i++)
	{
		%total += getWord(getField(%bar,%i),1);
	}

	if(%total <= 0)
	{
		return "";
	}

	%t = "";
	%remaining = %len;
	for(%i = 0; %i < %fCount; %i++)
	{
		%field = getField(%bar,%i);
		%t = %t @ "<color:" @ getWord(%field,2) @ ">";
		%cur = getMin(getMax(getWord(%field,0),1),getWord(%field,1));
		%chars = mRound(%len * (%cur / %total));
		for(%j = 0; %j < %chars; %j++)
		{
			%t = %t @ %b;
		}

		%remaining -= %chars;
	}

	%t = %t @ "<color:" @ %emptyColor @ ">";
	for(%i = 0; %i < %remaining; %i++)
	{
		%t = %t @ %b;
	}

	return %t;
}

function PBHud_Update(%cl)
{
	%pl = %cl.Player;

	if(!isObject(%pl) || %pl.getDamagePercent() >= 1.0)
		%pl = %cl.camera.getOrbitObject();

	%I = "0.3 1.0 0.3";
	%M = "1.0 0.3 0.1";
	%U = "0.25";
	%len = 60;

	%b = ".";

	%O = "444444";
	%Z = "FFFFFF";

	%FN = "<font:impact:30>";
	%NF = "<font:arial:17>";

	%h = 0.0;
	if(isObject(%pl))
	{
		%db = %pl.getDatablock();
		%h = (%db.maxDamage - %pl.getDamageLevel()) / %db.maxDamage;
	}

	if(isPackage(CustomHealth) && $NewHealth::Enabled)
	{
		if(strLen(%pl.health))
			%h = %pl.health / %pl.getMaxHealth();
	}

	if(%pl.tf2Overheal > 0)
	{
		%h = %h + %pl.tf2Overheal;
	}

	%h = mFloatLength(%h, 2);

	%X = rgb2hex(coLerp(%M, %I, ((%h - 0.5) * ((%U * 2)+1)) + 0.5));

	if((isObject(PlayerTF2UberImage) && %pl.getMountedImage(3) == PlayerTF2UberImage.getID()) || %pl.usingUber)
	{
		%h = 1;
		%X = rgb2hex(coLerp(%M, %I, (mSin($Sim::Time * 3.5) + 1) / 2));
	}

	if(isPackage(swol_downed))
	{
		if(%pl.isDowned)
		{
			%h = mFloatLength(%pl.downed_psuedoHealth / 100, 2); // nice typo, swollow! :egg:
			%X = rgb2hex(%M);
		}
	}
	%health = (%h * %pl.getDatablock().maxDamage) SPC %pl.getDatablock().maxDamage SPC %X;
	%armor = %pl.HealthArmorImage_armorHealth SPC %pl.getMountedImage(1).armorHealthMax SPC "7777ff";
	%hs = %h * 100 @ "% HP";
	if(%pl.HealthArmorImage_armorHealth !$= "")
	{
		%as = mCeil((%pl.HealthArmorImage_armorHealth / %pl.getMountedImage(1).armorHealthMax) * 100) @ "% AP";
	}
	%t = "<just:center>" @ %NF @ "\c6" @ %hs SPC %FN @ PBHud_GenerateBar(%health TAB %armor,%O) @ %NF @ "\c6" SPC %as;
	%t = %t @ "<br><just:center>";

	%item = %pl.tool[%pl.currTool];
	if(isObject(%item))
	{
		%ii = %pl.GetItemInstance(%pl.currTool);
		%maxUses = %item.image.maxUses;
		if(%maxUses !$= "" && %maxUses < 100)
		{
			%usesRemaining = %ii.get("usesRemaining");
			if(%usesRemaining $= "")
			{
				%usesRemaining = %maxUses;
			}

			%t = %t @ "<color:" @ %Z @ ">" @ %NF @ %usesRemaining @ "/" @ %maxUses SPC "USES";
		}

		if(%item.getId() == RiotTTShieldItem.getId())
		{
			%health = %ii.get("Health") || $Pref::Server::TT::ShieldDurability;
			%t = %t @ "<color:" @ %Z @ ">" @ %NF @ mCeil((%health / $Pref::Server::TT::ShieldDurability) * 100) @ "% SHIELD";
		}
	}
	
	%cl.bottomPrint(%t, 3, true);
}

function floatLerp(%from, %to, %at)
{
	%at = mClampF(%at, 0, 1.0);
	%to = mClampF(%to, 0, 1.0);
	%from = mClampF(%from, 0, 1.0);
	return mClampF(((%to - %from) * %at) + %from, 0, 1.0);
}

function coLerp(%from, %to, %at)
{
	%r = floatLerp(getWord(%from, 0), getWord(%to, 0), %at);
	%g = floatLerp(getWord(%from, 1), getWord(%to, 1), %at);
	%b = floatLerp(getWord(%from, 2), getWord(%to, 2), %at);

	return (%r SPC %g SPC %b);
}

function rgb2hex( %rgb )
{
	%r = comp2hex( 255 * getWord( %rgb, 0 ) );
	%g = comp2hex( 255 * getWord( %rgb, 1 ) );
	%b = comp2hex( 255 * getWord( %rgb, 2 ) );
 
	return %r @ %g @ %b;
}

function comp2hex( %comp )
{
	%left = mFloor( %comp / 16 );
	%comp = mFloor( %comp - %left * 16 );
	
	%left = getSubStr( "0123456789ABCDEF", %left, 1 );
	%comp = getSubStr( "0123456789ABCDEF", %comp, 1 );
	
	return %left @ %comp;
}