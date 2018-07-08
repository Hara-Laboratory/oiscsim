section common
{
    .constant DEC = 1;
    .constant Z = 0;
    .constant INC = -1;
    .constant MIN = 0x80000000;
    .constant WIDTH = 32;
    .constant HALFWIDTH = 16;
	macro LOAD_INLINE(wordaddr, negoffset, out)
	{
		sng4 negoffset, wordaddr, L_addr;
		sng4 Z, L_addr: 0, out;
	}
	macro STORE_INLINE(wordaddr, negoffset, in)
	{
		sng4 negoffset, wordaddr, L_addr;
		sng4 Z, inZ, L_addr: 0;
	}
	macro INC_INLINE(inout)
	{
		sng4 INC, inout, inout;
	}
	macro ADD_INLINE(ina, inb, out)
	{
		.variable tmp;
		sng4 ina, Z, tmp;
		sng4 tmp, inb, out;
	}
	macro SUB_INLINE(ina, inb, out)
	{
		sng4 inb, ina, out;
	}
	macro SUMFROMZERO_INLINE(to, out)
	{
		macro LPINIT(cnt, times, StartLabel, FinlzLabel)
		{
			.variable TJ;
			sng4 times, Z, cnt, StartLabel; //cnt = -times
			sng4 Z, INC, INC, FinlzLabel; //Force jump to finalization
		}
		macro LPTAIL(cnt, StartLabel)
		{
			sng4 INC, cnt, cnt, StartLabel; //if (++cnt<0) continue
		}
		
		.variable counter;
		sng4 L: Z, Z, out;
		~LPINIT(counter, to, Lpbdy, Lpfnlz);
Lpbdy:	//Body
		~SUB_INLINE(out, counter, out);
		~LPTAIL(counter, Lpbdy);
Lpfnlz:	//Finalization
		sng4 Z, INC, INC;
	}
}