package armor_functions
{
	function Player::Damage( %this, %obj, %pos, %damage, %damageType )
	{
		%armorImage = %this.getMountedImage(1);
		if(%armorImage.className $= "HealthArmorImage" && %damage > 0 )
		{
			%protectedDamage = getMin(%damage,%this.HealthArmorImage_armorHealth);
			%damage -= %protectedDamage;
		 	%armorHealth = %this.HealthArmorImage_armorHealth -= %protectedDamage;
			%protectedDamage = %protectedDamage * %armorImage.armorMultiplier;
			%damage += %protectedDamage;
			if(%armorHealth < 0.0001)
			{
				%this.playAudio(3,%armorImage.GetRandomSound("BreakSound"));
				%this.unmountImage(1);
			}
			else
			{
				%this.playAudio(3,%armorImage.GetRandomSound("HitSound"));
			}

			//commandToClient( %this.client, 'bottomprint', "<just:right><font:impact:21><color:dbd21f>Armor <font:impact:28>\c6" SPC MCeil( %this.armor ), 3, true );
		}

		parent::Damage( %this, %obj, %pos, %damage, %damageType );
	}
};
activatePackage( "armor_functions" );

function HealthArmorImage::OnMount(%db,%mount,%node)
{
	%mount.HealthArmorImage_armorHealth = %db.armorHealthMax;
	%mount.playAudio(3,%db.GetRandomSound("equipSound"));

	if(!%mount.getMountedImage(2))
	{
		%mount.mountImage(%db.hatImage,2);
	}
}

function HealthArmorImage::OnUnMount(%db,%mount,%node)
{
	%mount.HealthArmorImage_armorHealth = "";

	%hat = %mount.getMountedImage(2);
	if(%hat)
	{
		if(%mount.getMountedImage(2).getName() $= %db.hatImage)
		{
			%mount.unMountImage(2);
		}
	}
}

function HealthArmorImage::GetRandomSound(%db,%soundName)
{
	%soundCount = %db.armor[%soundName @ "count"];
	if(%soundCount $= "")
	{
		%c = 0;
		while(%db.armor[%soundName @ %c] !$= "")
		{
			%c++;
		}
		%soundCount = %db.armor[%soundName @ "count"] = %c;
	}

	return %db.armor[%soundName @ getRandom(0,%soundCount - 1)];
}

function fxDTSBrick::EquipHealthArmor(%brick,%armor,%client)
{
	%player = %client.player;
	if(isObject(%player) && isObject(%armor))
	{
		if(%armor.className $= "HealthArmorImage")
		{
			%player.unmountImage(1);
			%player.mountImage(%armor,1);
		}
	}
}
registerOutputEvent("fxDTSBrick", "EquipHealthArmor", "string 200 100", true);

function ProjectileData::Damage (%this, %obj, %col, %fade, %pos, %normal)
{
	if (%this.directDamage <= 0)
	{
		return;
	}
	%damageType = $DamageType::Direct;
	if (%this.DirectDamageType)
	{
		%damageType = %this.DirectDamageType;
	}
	%scale = getWord (%obj.getScale (), 2);
	%directDamage = %this.directDamage * %scale;
	if (%col.getType () & $TypeMasks::PlayerObjectType)
	{
		%col.Damage (%obj, %pos, %directDamage, %damageType);
	}
	else 
	{
		%col.Damage (%obj, %pos, %directDamage, %damageType);
	}
}

package Shield
{
	function ProjectileData::damage(%this,%obj,%col,%fade,%pos,%normal) 
	{
		%shielded = 0;
		if(%col.getType() & $TypeMasks::PlayerObjectType)
		{
			%image0 = %col.getMountedImage(0);
			%state0 = %col.getImageState(0);
			
			%scale = getWord(%col.getScale(),2);
			
			%fvec = %col.getForwardVector();
			%fX = getWord(%fvec,0);
			%fY = getWord(%fvec,1);
			
			%evec = %col.getEyeVector();
			%eX = getWord(%evec,0);
			%eY = getWord(%evec,1);
			%eZ = getWord(%evec,2);
			
			%eXY = mSqrt(%eX*%eX+%eY*%eY);
			
			%aimVec = %fX*%eXY SPC %fY*%eXY SPC %eZ;
			
			if(%image0 == shieldRiotTTImage.getID() && %state0 $= "Ready")
			{
			if(%eZ > 0.75)
				%shielded = (getword(%pos, 2) > getword(%col.getWorldBoxCenter(),2) - 3.3*%scale);
			else if(%ez < -0.75)
				%shielded = (getword(%pos, 2) < getword(%col.getWorldBoxCenter(),2) - 4.4*%scale);
			else
				%shielded = (vectorDot(vectorNormalize(%obj.getVelocity()),%aimVec) < 0);
			
			%damageScale = 0.05;
			%reflect = 1;
			%reflectVector = %aimVec;
			%reflectPoint = vectorAdd(%col.getHackPosition(),vectorScale(%reflectVector,vectorLen(%col.getVelocity())/5+1));
			%impulseScale = 0.3;
			if(%shielded)
				%col.spawnExplosion(hammerProjectile,getWord(%col.getScale(),2));
			}
		}
		
		if(getSimTime() - %obj.reflectTime < 500)
			%reflect = 0;

		%item = %col.tool[%col.currTool];
		if(%shielded && %item.getId() == RiotTTShieldItem.getId())
		{
			// -1 will be equivalent of infinity because TorqueScript's infinity sucks
			if($Pref::Server::TT::ShieldBreakBot || %col.getClassName() $= "Player")
			{
				if(isObject(%item))
				{	
					%ii = %col.GetItemInstance(%col.currTool);
					%health = %ii.get("Health");
					if(%health $= "")
					{
						%health = $Pref::Server::TT::ShieldDurability;
						%ii.set("Health",%health);
						
					}
					%health -= %this.directDamage;
					%ii.set("Health",%health);
					if(%health <= 0)
					{
						%col.spawnExplosion(shieldRiotTTProjectile,getWord(%col.getScale(),2));
						%col.tool[%col.currTool] = "";
						messageclient(%col.client,'MsgItemPickup','',%col.currTool,"");
						%col.unmountImage(0);
						ServerCmdDropTool(%col.client,%col.currTool);
					}
				}
			}

			//cancel radius damage on %obj
			%obj.damageCancel[%col] = 1;
			%obj.impulseScale[%col] = %impulseScale;
			%sound = getRandom(1,3);
			switch(%sound)
			{
				case 1:
					serverPlay3D(shieldHit2Sound,%pos);
				case 2:
					serverPlay3D(shieldHit1Sound,%pos);
				case 3:
					serverPlay3D(shieldHit1Sound,%pos);
				default:
					error("Invalid Sound");
			}
			
			if(%reflect)
			{
				%scaleFactor = getWord(%obj.getScale(), 2);
				%pos = %reflectPoint;
				%vec = vectorScale(%reflectVector,vectorLen(%obj.getVelocity()));
				%vel = vectorAdd(%vec,vectorScale(%col.getVelocity(),%obj.dataBlock.velInheritFactor));
				if(%col.getClassName() $= "AIPlayer")
				{
					%col.hName = %col.getPlayerName(); // hack to work with Slayer's Support_SpecialKills
				}
				%p = new Projectile()
				{
					dataBlock = %obj.dataBlock;
					initialPosition = %pos;
					initialVelocity = %vel;
					sourceObject = %col;
					client = %col.client;
					sourceSlot = 0;
					reflectTime = getSimTime();
				};
				MissionCleanup.add(%p);
				%p.setScale(%scaleFactor SPC %scaleFactor SPC %scaleFactor);
			}
			
			%obj.schedule(10, delete);
			
			//Special effect weapons like the Horse Ray will still affect you from the back
			if(%damageScale > 0)
			{
				%oldDmg = %this.directDamage;
				%this.directDamage *= %damageScale;
				%ret = Parent::damage(%this,%obj,%col,%fade,%pos,%normal);
				%this.directDamage = %oldDmg;
			}   
			return %ret;
		}
		else
		{
			return Parent::damage(%this,%obj,%col,%fade,%pos,%normal);
		}
	}
};