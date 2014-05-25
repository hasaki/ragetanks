using System;

[Flags]
public enum PlayerInput
{
	None	= 0x0000,
	Left	= 0x0001, 
	Right	= 0x0002,
	Jump	= 0x0004,
	Fire	= 0x0008
}
