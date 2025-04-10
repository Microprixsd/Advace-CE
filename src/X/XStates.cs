using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MMXOnline;

public class XHover : CharState {
	public SoundWrapper? sound;
	float hoverTime;
	int startXDir;
	public XHover() : base("hover", "hover_shoot") {
		airMove = true;
		attackCtrl = true;
		normalCtrl = true;
		useGravity = false;
	}

	public override void update() {
		base.update();

		character.xDir = startXDir;
		Point inputDir = player.input.getInputDir(player);

		if (inputDir.x == character.xDir) {
			if (!sprite.StartsWith("hover_forward")) {
				sprite = "hover_forward";
				shootSprite = sprite + "_shoot";
				character.changeSpriteFromName(sprite, true);
			}
		} else if (inputDir.x == -character.xDir) {
			if (player.input.isHeld(Control.Jump, player)) {
				if (!sprite.StartsWith("hover_backward")) {
					sprite = "hover_backward";
					shootSprite = sprite + "_shoot";
					character.changeSpriteFromName(sprite, true);
				}
			} else {
				character.xDir = -character.xDir;
				startXDir = character.xDir;
				if (!sprite.StartsWith("hover_forward")) {
					sprite = "hover_forward";
					shootSprite = sprite + "_shoot";
					character.changeSpriteFromName(sprite, true);
				}
			}
		} else {
			if (sprite != "hover") {
				sprite = "hover";
				shootSprite = sprite + "_shoot";
				character.changeSpriteFromName(sprite, true);
			}
		}

		if (character.vel.y < 0) {
			character.vel.y += Global.speedMul * character.getGravity();
			if (character.vel.y > 0) character.vel.y = 0;
		}

		if (character.gravityWellModifier > 1) {
			character.vel.y = 53;
		}

		hoverTime += Global.spf;
		if (hoverTime > 2 || player.input.checkDoubleTap(Control.Dash) ||
			stateFrames > 12 && player.input.isPressed(Control.Jump, player)
		) {
			character.changeState(new Fall(), true);
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.stopMovingWeak();
		startXDir = character.xDir;
		if (stateTime <= 0.1f) {
			sound = character.playSound("uahover", forcePlay: false, sendRpc: true);
		}
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		if (sound != null && !sound.deleted) {
			sound.sound?.Stop();
		}
		RPC.stopSound.sendRpc("uahover", character.netId);
	}
}

public class LightDash : CharState {
	public float dashTime;
	public float dustTime;
	public string initialDashButton;
	public int dashDir;
	public bool stop;
	public Anim? dashSpark;
	public Anim? exaust;

	public LightDash(string initialDashButton) : base("dash", "dash_shoot", "attack_dash") {
		enterSound = "dash";
		this.initialDashButton = initialDashButton;
		attackCtrl = true;
		normalCtrl = true;
	}

	public override void update() {
		base.update();
		if (!player.isAI && !player.input.isHeld(initialDashButton, player) && !stop) {
			dashTime = 900;
		}
		float inputXDir = player.input.getInputDir(player).x;

		if (dashTime > 37 || stop) {
			if (!stop) {
				if (exaust?.destroyed == false) {
					exaust.destroySelf();
				}
				dashTime = 0;
				character.frameIndex = 0;
				character.sprite.frameTime = 0;
				character.sprite.animTime = 0;
				character.sprite.frameSpeed = 0.1f;
				stop = true;
			} else {
				if (inputXDir != 0 && character.grounded) {
					character.changeState(new Run(), true);
				} else {
					character.changeState(new DashEnd());
				}
				return;
			}
		}
		if (dashTime > 3 || stop) {
			character.move(new Point(character.getDashSpeed() * 1.15f * dashDir, 0));
		} else {
			character.move(new Point(Physics.DashStartSpeed * character.getRunDebuffs() * dashDir * 1.15f, 0));
		}

		if (dashTime <= 3 || stop) {
			if (inputXDir != 0 && inputXDir != dashDir) {
				character.xDir = (int)inputXDir;
				dashDir = character.xDir;
			}
		}

		if (!character.grounded) {
			character.dashedInAir++;
			character.changeState(new Fall());
		}

		if (exaust == null && dashTime > 3 && !stop) {
			exaust = new Anim(
				character.pos.addxy(-15 * dashDir, -7),
				"fakezero_exhaust", dashDir, player.getNextActorNetId(),
				false, sendRpc: true, zIndex: character.zIndex - 100, host: character 
			);
		}

		if (dustTime >= 6 && !character.isUnderwater()) {
			dustTime = 0;
			new Anim(
				character.getDashDustEffectPos(dashDir),
				"dust", dashDir, player.getNextActorNetId(), true,
				sendRpc: true
			);
		} else {
			dustTime += character.speedMul;
		}
		dashTime += character.speedMul;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		dashDir = character.xDir;
		character.isDashing = true;
		character.globalCollider = character.getDashingCollider();
		dashSpark = new Anim(
			character.getDashSparkEffectPos(dashDir),
			"dash_sparks", dashDir, player.getNextActorNetId(),
			true, sendRpc: true
		);
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		if (dashSpark?.destroyed == false) {
			dashSpark.destroySelf();
		}
		if (exaust?.destroyed == false) {
			exaust.destroySelf();
		}
	}
}

public class GigaAirDash : CharState {
	public float dashTime;
	public string initialDashButton;
	public int dashDir;
	public bool stop;
	public Anim? dashSpark;
	public Anim? exaust = null!;

	public GigaAirDash(string initialDashButton) : base("dash", "dash_shoot", "attack_dash") {
		enterSound = "airdashX2";
		this.initialDashButton = initialDashButton;
		attackCtrl = true;
		normalCtrl = true;
		useGravity = false;
	}

	public override void update() {
		base.update();
		if (!player.isAI && !player.input.isHeld(initialDashButton, player) && !stop) {
			dashTime = 900;
		}
		float inputXDir = player.input.getInputDir(player).x;

		if (dashTime > 37 || stop) {
			if (!stop) {
				if (exaust?.destroyed == false) {
					exaust.destroySelf();
				}
				dashTime = 0;
				character.frameIndex = 0;
				character.sprite.frameTime = 0;
				character.sprite.animTime = 0;
				character.sprite.frameSpeed = 0.1f;
				stop = true;
			} else {
				if (inputXDir != 0 && character.grounded) {
					character.changeState(new Run(), true);
				} else {
					character.changeState(new DashEnd());
				}
				return;
			}
		}
		if (dashTime > 3 || stop) {
			character.move(new Point(character.getDashSpeed() * 1.15f * dashDir, 0));
		} else {
			character.move(new Point(character.getRunSpeed() * dashDir * 1.15f, 0));
		}
		if (exaust == null && dashTime > 3 && !stop) {
			exaust = new Anim(
				character.pos.addxy(-15 * dashDir, -7),
				"fakezero_exhaust", dashDir, player.getNextActorNetId(),
				false, sendRpc: true, zIndex: character.zIndex - 100, host: character 
			);
		}

		if (dashTime <= 3 || stop) {
			if (inputXDir != 0 && inputXDir != dashDir) {
				character.xDir = (int)inputXDir;
				dashDir = character.xDir;
			}
		}
		dashTime += character.speedMul;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.dashedInAir++;
		dashDir = character.xDir;
		character.isDashing = true;
		character.globalCollider = character.getDashingCollider();
		dashSpark = new Anim(
			character.getDashSparkEffectPos(dashDir),
			"dash_sparks", dashDir, player.getNextActorNetId(),
			true, sendRpc: true
		);
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		if (dashSpark?.destroyed == false) {
			dashSpark.destroySelf();
		}
		if (exaust?.destroyed == false) {
			exaust.destroySelf();
		}
	}
}


public class UpDash : CharState {
	public float dashTime = 0;
	public string initialDashButton;

	public UpDash(string initialDashButton) : base("up_dash", "up_dash_shoot") {
		this.initialDashButton = initialDashButton;
		attackCtrl = true;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.isDashing = true;
		character.useGravity = false;
		character.vel = new Point(0, -4);
		character.dashedInAir++;
		character.frameSpeed = 2;
	}

	public override void onExit(CharState newState) {
		character.useGravity = true;
		character.vel.y = 0;
		base.onExit(newState);
	}

	public override void update() {
		base.update();
		if (!player.input.isHeld(initialDashButton, player)) {
			character.changeState(new Fall());
			return;
		}

		if (!once) {
			once = true;
			character.vel = new Point(0, -250);
			new Anim(character.pos.addxy(0, -10), "dash_sparks_up", character.xDir, player.getNextActorNetId(), true, sendRpc: true);
			character.playSound("dash", sendRpc: true);
		}

		dashTime += Global.spf;
		float maxDashTime = 0.4f;
		if (character.isUnderwater()) maxDashTime *= 1.5f;
		if (dashTime > maxDashTime) {
			character.changeState(new Fall());
			return;
		}
	}
}
   
    public class XUPUpShot : CharState
{
	public Projectile busterUnpoUpProj;

	public float Time;

	public XUPUpShot()
		: base("unpo_up_air_shot", "", "", "")
	{
		enterSound = "buster2";
	}

	public override void onEnter(CharState oldState)
	{
		base.onEnter(oldState);
		busterUnpoUpProj = new BusterUnpoUpProj(base.player.weapon, character.getShootPos(), character.getShootXDir(), base.player, base.player.getNextActorNetId(), rpc: true);
		new Anim(character.getShootPos(), "buster_unpo_up_muzzle", character.getShootXDir(), base.player.getNextActorNetId(), destroyOnEnd: true, sendRpc: true);
		if (character.grounded)
		{
			character.changeSpriteFromName("unpo_up_shot", resetFrame: true);
		}
	}

	public override void onExit(CharState newState)
	{
		base.onExit(newState);
		busterUnpoUpProj.destroySelf();
	}

	public override void update()
	{
		base.update();
		Time += Global.spf;
		if (!character.grounded)
		{
			airCode();
		}
		if ((double)Time > 0.2)
		{
			character.changeState(new Idle());
		}
	}
}
   
    public class XUPDownShot : CharState
{
	public Projectile busterUnpoDownProj;

	public float Time;

	public XUPDownShot()
		: base("unpo_down_shot", "", "", "")
	{
		enterSound = "buster2";
	}

	public override void onEnter(CharState oldState)
	{
		base.onEnter(oldState);
		busterUnpoDownProj = new BusterUnpoDownProj(base.player.weapon, character.pos, character.xDir, base.player, base.player.getNextActorNetId(), rpc: true);
		new Anim(character.getShootPos(), "buster_unpo_down_muzzle", character.getShootXDir(), base.player.getNextActorNetId(), destroyOnEnd: true, sendRpc: true);
	}

	public override void onExit(CharState newState)
	{
		base.onExit(newState);
		busterUnpoDownProj.destroySelf();
		character.useGravity = true;
		character.airAttackCooldown = 2f;
	}

	public override void update()
	{
		base.update();
		if (base.player != null)
		{
			if (!once)
			{
				once = true;
				character.vel = new Point(0f, -300f);
			}
			Time += Global.spf;
			if ((double)Time > 0.2)
			{
				character.changeState(new Fall());
			}
		}
	}
}

public class X2ChargeShot : CharState {
	bool fired;
	int shootNum;
	bool pressFire;
	Weapon? weaponOverride;
	Weapon weapon => (weaponOverride ?? mmx.currentWeapon ?? mmx.specialBuster);
	MegamanX mmx = null!;

	public X2ChargeShot(Weapon? weaponOverride, int shootNum) : base("x2_shot") {
		this.shootNum = shootNum;
		this.weaponOverride = weaponOverride;
		useDashJumpSpeed = true;
		airMove = true;
		canStopJump = true;
		landSprite = "x2_shot";
		airSprite = "x2_air_shot";
		canJump = true;
		if (shootNum == 1) {
			sprite = "x2_shot2";
			defaultSprite = sprite;
			landSprite = sprite;
			airSprite = "x2_air_shot2";
		}
	}

	public override void update() {
		base.update();
		if (!fired && character.currentFrame.getBusterOffset() != null) {
			fired = true;
			if (shootNum == 0) {
				weapon.shoot(mmx, [4, 0]);
				weapon.shootCooldown = weapon.fireRate;
				mmx.shootCooldown = weapon.fireRate;
				if (weapon.shootSounds[3] != "") {
					character.playSound(weapon.shootSounds[3], sendRpc: true);
				}
				weapon.addAmmo(-weapon.getAmmoUsageEX(3, character), player);
				mmx.stockedTime = 0;
			} else {
				mmx.stockedX2Charge = false;
				weapon.shoot(mmx, [4, 1]);
				weapon.shootCooldown = weapon.fireRate;
				mmx.shootCooldown = weapon.fireRate;
				if (weapon.shootSounds[3] != "") {
					character.playSound(weapon.shootSounds[3], sendRpc: true);
				}
				weapon.addAmmo(-weapon.getAmmoUsageEX(3, character), player);
				mmx.stockedTime = 0;
			}
		}
		if (character.isAnimOver()) {
			if (shootNum == 0 && pressFire) {
				fired = false;
				shootNum = 1;
				sprite = "x2_shot2";
				defaultSprite = sprite;
				landSprite = sprite;
				airSprite = "x2_air_shot2";
				if (!character.grounded || character.vel.y < 0) {
					sprite = "x2_air_shot2";
				}
				character.changeSpriteFromName(sprite, true);
			} else {
				character.changeToIdleOrFall();
			}
		} else if (!pressFire && stateTime > Global.spf && player.input.isPressed(Control.Shoot, player)) {
			pressFire = true;
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		mmx = character as MegamanX ?? throw new NullReferenceException();

		if (oldState is AirDash or GigaAirDash or UpDash) {
			if (player.input.isPressed(Control.Jump, player)) {
				character.isDashing = false;
				character.vel.y = -character.getJumpPower();
				if (character.dashedInAir > 0) {
					character.dashedInAir--;
				}
			}
		}

		if (!character.grounded || character.vel.y > 0) {
			if (shootNum == 0) {
				sprite = "x2_air_shot";
			} else {
				sprite = "x2_air_shot2";
			}
		}
		if (mmx.chargedTornadoFang != null) {
			mmx.chargedTornadoFang = null;
		}
		character.changeSpriteFromName(sprite, true);
	}

	public override void onExit(CharState newState) {
		if (mmx.hasLastingProj()) {
			character.shootAnimTime = 4;
		} else if (newState is not AirDash and not WallSlide) {
			character.shootAnimTime = 0;
		}  else {
			character.shootAnimTime = 20 - character.animTime;
		}
		mmx.lastShootPressed = 100;
		mmx.stockedTime = 0;
		base.onExit(newState);
	}
}

public class X3ChargeShot : CharState {
	bool fired;
	public int state = 0;
	bool pressFire;
	MegamanX mmx = null!;
	public HyperCharge? hyperBusterWeapon;

	public X3ChargeShot(HyperCharge? hyperBusterWeapon) : base("x3_shot") {
		this.hyperBusterWeapon = hyperBusterWeapon;
		useDashJumpSpeed = true;
		airMove = true;
		canStopJump = true;
		landSprite = "x3_shot";
		airSprite = "x3_air_shot";
		canJump = true;
	}

	public override void update() {
		base.update();
		if (character.grounded) {
			character.turnToInput(player.input, player);
		}
		if (!fired && character.currentFrame.getBusterOffset() != null && player.ownedByLocalPlayer) {
			fired = true;
			if (state == 0) {
				Point shootPos = character.getShootPos();
				int shootDir = character.getShootXDir();
				if (!mmx.hasUltimateArmor) {
					new Anim(
						shootPos, "buster4_x3_muzzle", shootDir,
						player.getNextActorNetId(), true, sendRpc: true
					);
					new Buster4MaxProj(
						shootPos, shootDir,
						mmx, player, player.getNextActorNetId(), true
					);
					if (!(player.weapon is HyperCharge)) {
						character.playSound("buster3X3", sendRpc: true);
					}
				} else {
					new Anim(shootPos, "buster4_muzzle_flash", shootDir, null, true);
					new BusterPlasmaProj(
						shootPos, shootDir, mmx,
						player, player.getNextActorNetId(), rpc: true
					);
					character.playSound("plasmaShot", sendRpc: true);
				}
			mmx.stockedTime = 0;
			} else {
				if (hyperBusterWeapon != null) {
					hyperBusterWeapon.ammo -= hyperBusterWeapon.getChipFactoredAmmoUsage(player);
				}
				character.playSound("buster3X3", sendRpc: true);
				
			}
			mmx.stockedTime = 0;
		}
		if (character.isAnimOver()) {
			if (state == 0 && pressFire) {
				if (hyperBusterWeapon != null) {
					if (hyperBusterWeapon.ammo < hyperBusterWeapon.getChipFactoredAmmoUsage(player)) {
						character.changeToIdleOrFall();
						return;
					}
				} else {
					mmx.stockedX3Charge = false;
				}
				sprite = "x3_shot2";
				landSprite = "x3_shot2";
				if (!character.grounded || character.vel.y < 0) {
					sprite = "x3_air_shot2";
					defaultSprite = sprite;
				}
				defaultSprite = sprite;
				character.changeSpriteFromName(sprite, true);
				state = 1;
				fired = false;
			} else {
				character.changeToIdleOrFall();
			}
		} else {
			if (!pressFire && stateTime > Global.spf && player.input.isPressed(Control.Shoot, player)) {
				pressFire = true;
			}
			if (character.grounded && player.input.isPressed(Control.Jump, player)) {
				character.vel.y = -character.getJumpPower();
				if (state == 0) {
					sprite = "x2_air_shot";
					defaultSprite = sprite;
				} else {
					sprite = "x2_air_shot2";
					defaultSprite = sprite;
				}
				character.changeSpriteFromName(sprite, false);
			}
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		mmx = character as MegamanX ?? throw new NullReferenceException();
		if (mmx == null) {
			throw new NullReferenceException();
		}
		
		if (oldState is AirDash or GigaAirDash or UpDash) {
			if (player.input.isPressed(Control.Jump, player)) {
				character.isDashing = false;
				character.vel.y = -character.getJumpPower();
				if (character.dashedInAir > 0) {
					character.dashedInAir--;
				}
			}
		}
		if (!mmx.stockedX3Charge) {
			if (hyperBusterWeapon == null) {
				mmx.stockedX3Charge = true;
			}
			sprite = "x3_shot";
			defaultSprite = sprite;
			landSprite = "x3_shot";
			if (!character.grounded) {
				sprite = "x3_air_shot";
			}
			character.changeSpriteFromName(sprite, true);
		} else {
			mmx.stockedX3Charge = false;
			state = 1;
			sprite = "x3_shot2";
			defaultSprite = sprite;
			landSprite = "x3_shot2";
			if (!character.grounded) {
				sprite = "x3_air_shot2";
			}
			character.changeSpriteFromName(sprite, true);
		}
	}

	public override void onExit(CharState newState) {
		if (state == 0) {
			mmx.stockedX3Charge = true;
		} else {
			mmx.stockedX3Charge = false;
		}
		character.shootAnimTime = 0;
		mmx.stockedTime = 0;
		base.onExit(newState);
	}
}
