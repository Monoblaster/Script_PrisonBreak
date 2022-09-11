function Player::ItemHolding_hold(%player,%item)
{
	if(%player.ItemHolding_holding)
	{
		return "";
	}

	%toolSlot = %player.getDatablock().maxTools;
	%player.tool[%toolSlot] = %item.getDatablock().getId();

	%tempSlot = %player.currTool;
	serverCmdUseTool(%player.client,%toolSlot);
	%player.ItemHolding_currTool = %tempSlot;
	
	%player.ItemHolding_holding = true;
}

function Player::ItemHolding_unhold(%player)
{
	%toolSlot = %player.getDatablock().maxTools;
	%player.ItemHolding_holding = false;
	ServerCmdDropTool(%player.client, %toolSlot);
	serverCmdUseTool(%player.client,%player.ItemHolding_currTool);

}

package ItemHolding
{
	function ShapeBase::pickup(%this, %obj, %amount)
	{
		%slot = %obj.getDatablock().getSlot(%this);

		%r = parent::pickup(%this, %obj, %amount);
		if(%slot $= "")
		{
			%this.ItemHolding_hold(%obj);
			if(%obj.isStatic())
			{
				%obj.respawn();
			}
			else
			{
				%obj.delete();
			}
		}
		return %r;
	}
	
	function Armor::onTrigger(%this, %obj, %triggerNum, %val)
	{
		if (%triggerNum == 4 && %val && %obj.ItemHolding_holding)
		{
			%obj.ItemHolding_swap();
		}

		return parent::onTrigger(%this, %obj, %triggerNum, %val);
	}

	function ServerCmdUseTool(%client, %slot)
	{
		if(!%client.player.ItemHolding_holding)
		{
			return parent::ServerCmdUseTool(%client, %slot);
		}

		%client.player.ItemHolding_currTool = %slot;
		%client.player.ItemHolding_unhold();

		return "";
	}

	function ServerCmdUnUseTool(%client)
	{
		if(!%client.player.ItemHolding_holding)
		{
			return parent::ServerCmdUnUseTool(%client);
		}

		%client.player.ItemHolding_currTool = -1;

		return "";
	}

	function ServerCmdDropTool(%client, %position)
	{
		if(!%client.player.ItemHolding_holding)
		{
			return parent::ServerCmdDropTool(%client, %position);
		}

		if(%client.player.ItemHolding_currTool  != -1)
		{
			%client.player.ItemHolding_currTool = %position;
		}
		%client.player.ItemHolding_unhold();

		return "";
	}

	function Armor::OnDisabled(%db,%Obj)
	{
		if(%Obj.ItemHolding_holding)
		{
			%Obj.ItemHolding_unhold();
		}

		return Parent::OnDisabled(%db,%Obj);
	}
};
deactivatePackage("ItemHolding");
activatePackage("ItemHolding");