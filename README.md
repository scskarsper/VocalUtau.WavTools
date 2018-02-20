# VocalUtau.WavTools
This Project is Also Called WavToolSharp

A Realtime Likely WavTools (Buffered WavStream) for UTAU Engine. based NAudio and reference a bit form wavtool-pl.

Now It is a replacement component for wavtool.exe of UTAU song synthesizer.

#Include
A full code to make wavtool work as stream,All work on the main Assembly:VocalUtau.WavTools
Compiler it use ConsoleApplication Mode,It will work well as wavtoolsharp.exe

A BufferedPlayer and A PairPipe C/S Engine is also include.
VocalUtau.Wavtools.BPlayer and VocalUtau.Wavtools.Client is a demo of it,show about how to render a UTAU realtime likely.

#Special
WavTool Sharp can use --options- and --command- prefix to send or set options to engine.(If you want to recieve~)
WavTool Sharp can recieve more than 32767 ControlPoint of Wave Envlopes. p1~p5 is work as same as Normal UTAU.and if you want to send more setting,continue input keypair p6 v6 p7 v7... they will work as another control point after p5.
It means,your controlpoint sort is {Start}-p1(default,0)-p2-p5-p6-p7-...pn-p3-p4(default,0)-{END}
