function ItemData::getSlot(%iDB,%player)
{
	%pDB = %player.getDataBlock();
	%slotCount = %pDB.maxTools;
	%itemTags = %iDB.ItemSlots_Tags;
	%itemTagsCount = getWordCount(%itemTags);

	if(%iDB.ItemSlots_Tags $= "")
	{
		for(%i = 0; %i < %slotCount; %i++)
		{
			if(%pDB.ItemSlots_Tag[%i] $= "" && !isObject(%player.tool[%i]))
			{
				return %i;
			}
		}
	}

	for(%i = 0; %i < %slotCount; %i++)
	{
		if(isObject(%player.tool[%i]))
		{
			continue;
		}

		%currSlotTag = 	%pDb.ItemSlots_Tag[%i];
		for(%j = 0; %j < %itemTagsCount; %j++)
		{
			%currItemTag = getWord(%itemTags,%j);
			if(%currSlotTag $= %currItemTag)
			{
				return %i;
			}
		}
	}

	return "";
}

function ItemData::getSlotTag(%iDB,%player)
{
	%pDB = %player.getDataBlock();
	%slotCount = %pDB.maxTools;
	%itemTags = %iDB.ItemSlots_Tags;
	%itemTagsCount = getWordCount(%itemTags);

	for(%i = 0; %i < %slotCount; %i++)
	{
		%currSlotTag = 	%pDb.ItemSlots_Tag[%i];
		for(%j = 0; %j < %itemTagsCount; %j++)
		{
			%currItemTag = getWord(%itemTags,%j);
			if(%currSlotTag $= %currItemTag)
			{
				return %currSlotTag;
			}
		}
	}

	return "";
}

function ItemData::onPickup (%this, %obj, %user, %amount)
{
	if (%obj.canPickup == 0)
	{
		return 0;
	}

	// Get slot
	%slot = %this.getSlot(%user);

	if(%slot !$= "")
	{
		%obj.delete ();
		%user.tool[%slot] = %this;
		if (%user.client)
		{
			messageClient (%user.client, 'MsgItemPickup', '', %slot, %this.getId());
		}
		return 1;
	}

	//create error message
	if (%user.client)
	{
		%tag = %this.getSlotTag(%user);
		if(%tag $= "")
		{
			%user.client.centerprint("\c6You do not have a slot for this item.",2);
		}
		else
		{
			%user.client.centerprint("\c6Your\c3" SPC %this.getSlotTag(%user) SPC "\c6slot is full.",2);
		}
		
	}
	

	return 0;
}

function Player::ItemSlots_UpdateEmptySlots(%player)
{
	%pDB = %player.getDataBlock();
	%slotCount = %pDB.maxTools;
	for(%i = 0; %i < %slotCount; %i++)
	{
		if(isObject(%player.tool[%i]))
		{
			continue;
		}

		if(!isObject(%pDB.ItemSlots_EmptyItem[%i]))
		{
			continue;
		}

		%player.ItemSlots_MessageItemBypass = true;
		messageClient(%player.client,'MsgItemPickup','',%i,%pDB.ItemSlots_EmptyItem[%i].GetId(),true);
		%player.ItemSlots_MessageItemBypass = false;
	}
}

package ItemSlots
{
	function Armor::OnAdd(%db,%obj)
	{
		%obj.ItemSlots_UpdateEmptySlots();
		return parent::OnAdd(%db,%obj);
	}

	function messageClient(%client,%msgType,%msgString,%a1,%a2,%a3,%a4,%a5,%a6,%a7,%a8,%a9,%a10,%a11,%a12,%a13)
	{
		%r = parent::messageClient(%client,%msgType,%msgString,%a1,%a2,%a3,%a4,%a5,%a6,%a7,%a8,%a9,%a10,%a11,%a12,%a13);

		%player = %client.player;
		if(isObject(%player))
		{
			if(!%player.ItemSlots_MessageItemBypass && %msgType == getTag("MsgItemPickup"))
			{
				%player.ItemSlots_UpdateEmptySlots();
			}
		}

		return %r;
	}
};
activatepackage("ItemSlots");

function player::AddRandomItemm(%player, %colorID, %noDoMsg, %client)
{
	if(!isObject(%simSet = "AddRandomItemm_Cat" @ %colorID) || !%simSet.getCount())
	{
		return;
	}

	%count = %player.getDataBlock().maxTools;

	for(%i = 0; %i < %count; %i++)
	{
		if(!isObject(%player.tool[%i]))
		{
			%freeSlot = %i;
			break;
		}
	}

	if(%freeSlot $= "")
	{
		return;
	}

	%item = getRandomItemColorBased(%colorID);

	%itemObj = new Item()
	{
		dataBlock = %item;
	};
	%player.pickup(%itemObj,1);
	if(isObject(%itemObj))
	{
		%itemObj.delete();
	}

	if(%item.className $= "Weapon")
	{
		%player.weaponCount++;
	}

	if(!%noDoMsg)
	{
		%colorCat = $AddRandomItemm::ColorName[%colorID] !$= "" ? $AddRandomItemm::ColorName[%colorID] : getField(getColorIDTableCat(%colorID), 0) SPC "-" SPC getField(getColorIDTableCat(%colorID), 1);

		cancel(%client.AddRandomItemm_msgSched);

		%newLine = %client.AddRandomItemm_msg $= "" ? "" : "<br>";

		%client.AddRandomItemm_msg = %client.AddRandomItemm_msg @ %newLine @ "\c6+<color:" @ AddRandomItemm_rgbToHex(getColorIDTable(%colorID)) 
		@ ">" @ %item.uiName SPC "\c6[<color:" @ AddRandomItemm_rgbToHex(getColorIDTable(%colorID)) @ ">" @ %colorCat @ "\c6," SPC $AddRandomItemm::Rarity[%chosenRarity] @ "\c6]";

		%client.AddRandomItemm_msgSched = schedule(25, 0, eval, "commandToClient(" @ %client @ ", \'centerPrint\'," SPC %client @ ".AddRandomItemm_msg, 3);" SPC %client @ ".AddRandomItemm_msg = \"\";");
	}
}

function Player::addItemm(%player,%image,%client)
{
	%itemObj = new Item()
	{
		dataBlock = %image;
	};
	%player.pickup(%itemObj,1);
	if(isObject(%itemObj))
	{
		%itemObj.delete();
	}
}

registerOutputEvent("fxDtsBrick", "attemptInventoryCheck", "int 1 16 1" TAB "datablock ItemData", 1);
function fxDtsBrick::attemptInventoryCheck(%brk, %amt,%testitem, %cl)
{
	%pl = %cl.Player;

	if(!isObject(%pl))
		return;
	
	$inputTarget_Player = %pl;
	$inputTarget_Client = %cl;
	$inputTarget_Minigame = getMinigameFromObject(%pl);

	%success = false;

	%cts = 0;

	%dummy = new Player(){dataBlock = %pl.getDatablock();};
	%count = %pl.getDataBlock().maxtools;
	for(%i = 0; %i < %count; %i++)
	{
		%dummy.tool[%i] = %pl.tool[%i];
	}

	%success = true;
	for(%i = 0; %i < %amt; %i++)
	{
		%slot = %testitem.getSlot(%dummy);
		%dummy.tool[%slot] = %testitem;
		if(%slot $= "")
		{
			%success = false;
		}
	}
	%dummy.delete();

	if(%success)
		%brk.processInputEvent("onInventoryCheckSuccess",%cl);
	else
		%brk.processInputEvent("onInventoryCheckFail",%cl);
}

function serverCmdGiveItem(%cl, %t, %a, %b, %c, %d, %e, %f, %g, %h, %i, %j, %k)
{
	if(!%cl.isAdmin)
		return messageClient(%cl, '', "\c5You are not an admin.");

	%iName = stripMLControlChars(trim(%a SPC %b SPC %c SPC %d SPC %e SPC %f SPC %g SPC %h SPC %i SPC %j SPC %k));

	%cts = DataBlockGroup.getCount();
	for(%i = 0; %i < %cts; %i++)
	{
		%db = DataBlockGroup.getObject(%i);
		if(%db.getClassName() $= "ItemData" && stripos(%db.uiName, %iName) != -1)
		{
			%item = %db;
			break;
		}
	}

	if(!isObject(%item))
		return messageClient(%cl, '', "\c5Invalid item.");

	%target = findClientByName(%t);
	if(!isObject(%target.player))
		return messageClient(%cl, '', "\c5No player object found for" SPC %target.name @ ".");

	announce("<color:" @ $AUColorB @ ">" @ %target.name SPC "<spush><color:" @ $AUColorA @ ">has been given a(n)<spop>" SPC %Item.UIName SPC "<spush><color:" @ $AUColorA @ ">by<spop>" SPC %cl.name @ "<color:" @ $AUColorA @ ">.");
	echo(" - " @ %cl.name @ " gave " @ %target.name @ " a(n) " @ %item.uiName);
	%pl = %target.player;
	%itemObj = new Item(){dataBlock = %item;};
	%pl.pickup(%itemObj,1);
	if(isObject(%itemObj))
	{
		%itemObj.delete();
	}
}

function ServerCmdUseTool (%client, %slot)
{
	if (%client.isTalking)
	{
		serverCmdStopTalking (%client);
	}
	%player = %client.Player;
	if (!isObject (%player))
	{
		return;
	}
	if (%player.tool[%slot] > 0)
	{
		%player.currTool = %slot;
		%client.currInv = -1;
		%client.currInvSlot = -1;
		%item = %player.tool[%slot].getId ();
		%item.onUse (%player, %slot);
	}
	else
	{
		ServerCmdUnUseTool (%client);
	}
}

function gameConnection::DropInventory(%client)
{
    if(isObject(%client.player))
    {
        for(%i=0;%i<%client.player.getDatablock().maxTools;%i++)
        {    
            %item = %client.player.tool[%i];
            if(isObject(%item))
            {
                %pos = %client.player.getPosition();
                %rand = getRandom() * 3.14159 * 20;
                %x = mSin(%rand);
                %y = mCos(%rand);
                %offset = vectorNormalize(%x SPC %y);
                %vec = %client.player.getVelocity();
                %item = new Item()
                {
                    dataBlock = %item;
                    position = vectorAdd(%pos, %offset);
                };
                %itemVec = %vec;
                %itemVec = vectorAdd(%itemVec,"0 0 5");
                %item.BL_ID = %client.BL_ID;
                %item.minigame = %client.minigame;
                %item.spawnBrick = -1;
                %item.setVelocity(%itemVec);
                %item.schedulePop();
            }
            %client.player.tool[%i] = "";
        }
		ServerCmdDropTool(%client, 0);
    }
    Parent::DropInventory(%client);
}

function GameConnection::BuyItem(%client,%data,%cost)
{
	%cost = mFloor(%cost);
	%player = %client.player;
	if(!isObject(%player) || !isObject(%data))
	{
		return;
	}

	$InputTarget_["Player"] = %player;
	$InputTarget_["Client"] = %client;
	$InputTarget_["Minigame"] = getMinigameFromObject(%player);

	if(%client.score < %cost)
	{
		messageClient(%client,'',"\c5You don't have enough money for this");
		messageClient(%client,'',"\c5Requires \c6"@%cost@"$");
		$InputTarget_["Self"].processInputEvent("onPlayerBuyFailed", %client);
		return;
	}

	%itemObj = new Item(){dataBlock = %data;};
	%player.pickup(%itemObj,1);
	if(isObject(%itemObj))
	{
		messageClient(%client,'',"\c5You have no inventory space!");
		%itemObj.delete();
		return;
	}
	%client.score -= %cost;
	messageClient(%client,'',"<font:impact:30>\c5Purchased \c6"@%data.uiname@"\c5 for \c6"@%cost@"$");	
	$InputTarget_["Self"].processInputEvent("onPlayerBuy", %client);
}

function Player::WeaponAmmoPrint(%pl, %cl, %idx, %sit)
{
	return;
}
deactivatePackage("WeaponDropCharge");

package ItemSlotsClearDroppedItems
{
	function MiniGameSO::reset(%mini, %client)
	{
		%r = parent::reset(%mini, %client);

		%count = MissionCleanup.getCount();
		for(%i = %count - 1; %i >= 0; %i--)
		{
			%obj = MissionCleanup.getObject(%i);
			if(((%obj.getClassName() $= "Item" && !%obj.static) && %obj.miniGame == %mini) || %obj.getClassName() $= "Projectile") 
			{
				%obj.delete();
			}
		}

		return %r;
	}
};
activatePackage("ItemSlotsClearDroppedItems");