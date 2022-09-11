$GuardPay::WageDelay = 60000;
$GuardPay::WagePay = 1000;
$GuardPay::WageMessage = "\c6You recieved your minutely \c3$" @ $GuardPay::WagePay SPC "\c6paycheck.";

$GuardPay::KillPay = 1000;
$GuardPay::KillMessage = "\c6You recieved \c3$" @ $GuardPay::KillPay SPC "\c6for dispensing justice.";

function Player::guardPayWageLoop(%player)
{
	%client = %player.client;
	if(%client.slyrteam.name !$= "Guard")
	{
		return;
	}

	%client.chatMessage($GuardPay::WageMessage);
	%client.score += $GuardPay::WagePay;

	%player.guardPayLoop = %player.schedule($GuardPay::WageDelay, "guardPayWageLoop");
}

function Player::guardPayKill(%player)
{
	%client = %player.client;
	if(%client.slyrteam.name !$= "Guard")
	{
		return;
	}

	%client.chatMessage($GuardPay::KillMessage);
	%client.score += $GuardPay::KillPay;
}

package guardPay
{
	function Armor::OnAdd(%db,%obj)
	{
		%r = parent::OnAdd(%db,%obj);
		if(!isEventPending(%obj.guardPayLoop))
		{
			%obj.guardPayLoop = %obj.schedule($GuardPay::WageDelay, "guardPayWageLoop");
		}
		return %r;
	}

	function GameConnection::onDeath(%client, %sourceObject, %sourceClient, %damageType, %damLoc)
	{
		%player = %sourceClient.player;
		if(%client.slyrteam.name $= "Prisoner" && isObject(%player))
		{
			%player.guardPayKill();
		}
		return parent::onDeath(%client, %sourceObject, %sourceClient, %damageType, %damLoc);
	}
};
activatePackage("guardPay");