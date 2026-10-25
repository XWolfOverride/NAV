{
 NAV: A fast mode to navigate between folders and execute files

 Copyright 2019-2021 XWolfOverride

 Licensed under the MIT License

 Permission is hereby granted, free of charge, to any person obtaining a copy of this software
 and associated documentation files (the "Software"), to deal in the Software without restriction,
 including without limitation the rights to use, copy, modify, merge, publish, distribute,
 sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
 furnished to do so, subject to the following conditions:

 The above copyright notice and this permission notice shall be included in all copies or
 substantial portions of the Software.

 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
 BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
}

{Compile using Turbo Pascal 7}

Program fast_navigator;

{{$DEFINE LOWRAM}
{$DEFINE TEXT_READER} {WIP!!}
{$DEFINE INFOBOX}

{$DEFINE SPANISH}
{{$DEFINE ENGLISH}

{{$DEFINE DEBUG_KEY}
{{$DEFINE DEBUG_HIGH}
{{$DEFINE DEBUG_LOW}

{$IFDEF TEXT_READER}
{$M 4096,0,3500}
{$ELSE}
{$M 3072,0,0}
{$ENDIF}

uses TUI,DOS,XWMem;

{$IFDEF LOWRAM}
{$IFDEF TEXT_READER}
 NO SE PUEDEN ELEGIR LOS MODOS LOWRAM Y TEXT_READER A LA VEZ.
{$ENDIF}
{$IFDEF TEXT_READER}
 NO SE PUEDEN ELEGIR LOS MODOS LOWRAM E INFOBOX A LA VEZ.
{$ENDIF}
{$ENDIF}

type
  TFileName=string[12];
  TFileType=(ftDirectory,ftExecutable,ftBatch,ftOther);
  TFileRecord=record
    name:TFileName;
    kind:TFileType;
    {$IFDEF INFOBOX}
    size:longint;
    {$ENDIF}
  end;
  TCharA=array[0..65534] of char;
  PCharA=^TCharA;

const
  version:string[4]='1.2 ';
{$IFDEF SPANISH}
  lang='sp';
  { Space:   {==============================}
  HELP_ENTER='[Enter]   Navegar / Ejecutar';
  HELP_BACK= '[Borrar]  Carpeta superior';
  HELP_DIRS= '[Flechas] Seleccionar';          {}
  HELP_ESC=  '[Esc]     Salir';
  HELP_SEAR1='Usar el teclado para buscar';
  {$IFDEF INFOBOX}
  INFO_EXE='Ejecutable';
  INFO_BAT='Batch     ';
  INFO_OTR='Otros     ';
  {$ENDIF}
{$ENDIF}
{$IFDEF ENGLISH}
  lang='en';
  { Space:   {==============================}
  HELP_ENTER='[Enter]     Execute / Browse';
  HELP_BACK= '[Backspace] Upper folder';
  HELP_DIRS= '[Arrows]    Select';
  HELP_ESC=  '[Esc]       Exit';
  HELP_SEAR1='Use keyboard to do a fast search.';
  {$IFDEF INFOBOX}
  INFO_EXE='Executable';
  INFO_BAT='Batch     ';
  INFO_OTR='Others    ';
  {$ENDIF}
{$ENDIF}

{$IFDEF LOWRAM}
  MaxFiles=128;
{$ELSE}
{$IFDEF TEXT_READER}
  MaxFiles=1024;
  TextMemSize=3500;
{$ELSE}
  MaxFiles=512;
{$ENDIF}
{$ENDIF}
var
{$IFDEF LOWRAM}
{$ELSE}
  InitialScreen:array[0..4000] of word;
{$ENDIF}
  ScreenMode:TScreenMode;
  Dir:string;
  Files:Array[0..MaxFiles] of TFileRecord;
  Max:integer;
  Scroll,Selection:integer;
  Columns:byte;
  Search:TFileName;
{$IFDEF INFOBOX}
  InfoShown:boolean;
{$ENDIF}
  optionhidd:boolean;
  optionmono:boolean;
  optiondirs:boolean;
  attrBox:byte;
  attrItm:byte;
  attrIex:byte;
  attrIba:byte;
  attrIot:byte;
  attrSel:byte;
  attrSrc:byte;
  attrHelpBox:byte;

procedure DrawUI(full:boolean); forward;
procedure Init; forward;
procedure Done; forward;

{Helper for convert to string}
function cstr(val:byte):string;
var result:String;
begin
 str(val,result);
 cstr:=result;
end;

{Restore inital screen for the non main dialog part}
{$IFDEF LOWRAM}
{$ELSE}
procedure RestoreLeftSide;
var x,w,h:byte;
begin
  case ScreenMode of
  smLow:begin
    x:=30;
  end;
  smNormal:begin
    x:=46;
  end;
  smHigh:begin
    x:=61;
  end;
  end;
  w:=79-x;
  h:=ScreenInfo.height-4;
  RestorePartialScreen(InitialScreen,x,2,w,h);
  DrawUI(true);
end;
{$ENDIF}

procedure ShowHelpDialog;
var x,y:byte;
begin
{$IFDEF LOWRAM}
  case ScreenMode of
  smLow:begin
    x:=1;
    y:=6;
  end;
  smNormal:begin
    x:=5;
    y:=6;
  end;
  smHigh:begin
    x:=13;
    y:=18;
  end;
  end;
{$ELSE}
  case ScreenMode of
  smLow:begin
    x:=1;
    y:=6;
  end;
  smNormal:begin
    x:=21;
    y:=6;
  end;
  smHigh:begin
    x:=21;
    y:=18;
  end;
  end;
{$ENDIF}
  DrawBox(x,y,38,12,bmSingle,attrHelpBox,true);
  DrawString(x+2,y+1,'\  / Folder Navigator V'+version+lang,attrHelpBox);
  DrawString(x+2,y+2,' XX   By XWolf Override 2020-2023',attrHelpBox);
  DrawString(x+2,y+3,'/  \',attrHelpBox);
  DrawString(x+7,y+4,HELP_ENTER,attrHelpBox);
  DrawString(x+7,y+5,HELP_BACK,attrHelpBox);
  DrawString(x+7,y+6,HELP_DIRS,attrHelpBox);
  DrawString(x+7,y+7,HELP_ESC,attrHelpBox);

  DrawString(x+2,y+9,HELP_SEAR1,attrHelpBox);
{$IFDEF LOWRAM}
  DrawString(x+21,y+10,'Low RAM version',attrHelpBox);
{$ELSE}
{$IFDEF TEXT_READER}
  DrawString(x+19,y+10,'File view version',attrHelpBox);
{$ELSE}
{$ENDIF}
{$ENDIF}
  while getKey=#0 do;
{$IFDEF LOWRAM}
  FillScreen($0000);
  DrawUI(true);
{$ELSE}
  RestoreLeftSide;
  InfoShown:=false;
{$ENDIF}
end;

{$IFDEF INFOBOX}

Function FBytes(bytes:longint):string;
var
  tip:string[3];
begin
  tip:='by';
  if Bytes>=1024 then begin tip:='kb';Bytes:=Bytes shr 10;end;
  if Bytes>=1024 then begin tip:='mb';Bytes:=Bytes shr 10;end;
  if Bytes>=1024 then begin tip:='gb';Bytes:=Bytes shr 10;end; {<< MAX using a signed int of 32 (lonignt)}
  FBytes:=Cstr(Bytes)+Tip;
end;

procedure InfoHide;
var x:byte;
begin
  if InfoShown then begin
    case ScreenMode of
    smNormal:begin
      x:=55;
    end;
    smHigh:begin
      x:=62;
    end;
    end;
    RestorePartialScreen(InitialScreen,x,3,16,5);
    InfoShown:=false;
  end;
end;

procedure InfoShow(i:integer);
var x:byte;
begin
  if Files[i].kind=ftDirectory then begin
    InfoHide;
    exit;
  end;
  case ScreenMode of
  smLow:begin
    exit;
  end;
  smNormal:begin
    x:=55;
  end;
  smHigh:begin
    x:=62;
  end;
  end;
  if not InfoShown then begin
    DrawBox(x,3,16,5,bmSingle,attrBox,true);
    InfoShown:=true;
  end;
  DrawString(x+2,4,Files[i].Name,attrBox);
  case Files[i].kind of
    ftExecutable:
      DrawString(x+2,5,INFO_EXE,attrBox);
    ftBatch:
      DrawString(x+2,5,INFO_BAT,attrBox);
    ftOther:
      DrawString(x+2,5,INFO_OTR,attrBox);
  end;
  DrawString(x+2,6,FBytes(Files[i].Size)+'   ',attrBox);
end;

{$ENDIF}

{Return the name with the extension aligned}
function  Filename12(name:TFileName):TFileName;
var i,p:integer;
    ext:TFileName;
begin
 p:=Pos('.',name);
 if p>0 then begin
  ext:=Copy(name,p+1,3);
  name[0]:=#12;
  for i:=p to 12 do name[i]:=' ';
  Insert(ext,name,10);
 end else begin
  for i:=Length(name)+1 to 12 do name[i]:=' ';
  name[0]:=#12;
 end;
 Filename12:=name;
end;

{Add a record to the record list}
procedure AddRecord(name:TFileName;kind:TFileType;size:longint);
var
  i,p:integer;
  brk:boolean;
begin
  if Max=MaxFiles then
    exit;
  i:=0;
  while i<=Max do begin
    brk:=(kind<Files[i].kind) or
         ((kind=Files[i].kind) and (name<=Files[i].name));
    if brk then
    break
    else
    inc(i);
  end;
  p:=i;
  for i:=Max downto p do
    Files[i+1]:=Files[i];
  inc(Max);
  Files[p].name:=name;
  Files[p].kind:=kind;
  {$IFDEF INFOBOX}
  Files[p].size:=size;
  {$ENDIF}
end;

{Scan directory}
procedure ReadDirectory;
var
 t:searchrec;
 name:TFileName;
 ext:string[3];
 skip:boolean;
begin
 GetDir(0,Dir);
 Max:=-1;
 FindFirst('*.*',directory,T);
 while doserror=0 do begin
  skip:=false;
  if (t.name='.') or (t.name='..') then skip:=true;
  if (not optionhidd) and ((t.attr and hidden)<>0) then skip:=true;
  if optiondirs and ((t.attr and directory)=0) then skip:=true;
  if not skip then begin
   name:=Filename12(t.name);
   ext:=copy(name,10,3);
   if (t.attr and directory)<>0 then
    AddRecord(name,ftDirectory,t.size)
   else if (ext='EXE') or (ext='COM') then
    AddRecord(name,ftExecutable,t.size)
   else if ext='BAT' then
    AddRecord(name,ftBatch,t.size)
{$IFDEF TEXT_READER}
   else
    AddRecord(name,ftOther,t.size)
{$ENDIF}
   ;
  end;
  FindNext(t);
 end;
 Selection:=0;Scroll:=0;
end;

{$IFDEF TEXT_READER}
{Open a file for reading}
procedure FileView(path:string);
var
  f:file;
  buf:PCharA;
  readed,pos:word;
  x,y:byte;
begin
  GetMem(buf,TextMemSize);
  DrawBox(1,1,ScreenInfo.width-2,ScreenInfo.height-2,bmSingle,$5F,true);
  {$I-}
  Assign(f,path);
  FileMode:=0;
  Reset(f,1);
  BlockRead(f,buf^,TextMemSize,readed);
  {Draw screen with this shit}
  x:=2;
  y:=2;
  if readed > 0 then
  for pos:=0 to readed-1 do begin
    if (buf^[pos]<>#13) and (buf^[pos]<>#10) then
    DrawString(x,y,buf^[pos],$57);
    inc(x);
    if ((buf^[pos]=#10) and (pos>0) and (buf^[pos-1]=#13)) or (x>ScreenInfo.width-3) then begin
      x:=2;
      inc(y);
      if y>ScreenInfo.height-3 then
        break;
    end;
  end;
  GetKey;
  Close(f);
  {$I+}
  FreeMem(buf,TextMemSize);
  RestoreScreen(InitialScreen);
  {$IFDEF INFOBOX}
  InfoShown:=false;
  {$ENDIF}
  DrawUI(true);  
end;
{$ENDIF}

{Executes the selected file, if file is an executable executes it,
 if a directory reads it}
procedure Endrejuta(fil:TFileRecord);
var
 path:string;
 name:TFileName;
 i,p:integer;
 fattr:word;
begin
 Search:='';
 {Path preparation}
 path:=Dir;
 if path[length(path)]<>'\' then
  path:=path+'\';
 {Name standarization}
 name:=fil.name;
 p:=Pos(' ',name);
 if name[10]<>' ' then begin
  name[0]:=char(p);
  name[p]:='.';
  for i:=10 to 12 do
   if name[i]<>' ' then
    name:=name+name[i];
 end else
  name[0]:=char(p-1);
 {Detect directory or executable}
 path:=path+name;
 case fil.kind of
 ftDirectory:begin
  chdir(path);
  ReadDirectory;
  DrawUI(true);
 end;
 ftExecutable:begin
  Done;
  SwapVectors;
  Exec(path,'');
  SwapVectors;
  Init;
  ReadDirectory;
  {$IFDEF INFOBOX}
  InfoShown:=false;
  {$ENDIF}
  DrawUI(true);
 end;
 ftBatch:begin
  Done;
  SwapVectors;
  Exec(GetEnv('COMSPEC'),'/C '+path);
  SwapVectors;
  Init;
  ReadDirectory;
  {$IFDEF INFOBOX}
  InfoShown:=false;
  {$ENDIF}
  DrawUI(true);
 end;
{$IFDEF TEXT_READER}
 ftOther:begin
  FileView(path);
 end;
{$ENDIF}
 end;
end;

{Goes back on the directory listing}
procedure Patra;
var
 path:string;
begin
 {Path preparation}
 path:=Dir;
 if path[length(path)]<>'\' then
  path:=path+'\';
 {Change directory}
 path:=path+'..';
 {$I-}
 ChDir(path);
 {$I+}
 ReadDirectory;
 DrawUI(true);
end;

{Draw the UI for the current directory}
procedure DrawUI(full:boolean);
var
 x,y,w,h,rows,attr,col,row:byte;
 title:string[80];
 i,soffset:integer;
 charinfo:word;
 sinput:TFileName;
begin
  case ScreenMode of
  smLow:begin
    x:=1;
    y:=1;
    w:=29;
    h:=23;
    Columns:=2;
  end;
  smNormal:begin
    x:=2;
    y:=1;
    w:=44;
    h:=23;
    Columns:=3;
  end;
  smHigh:begin
    x:=2;
    y:=2;
    w:=59;
    h:=ScreenInfo.height-(y+2);
    Columns:=4;
  end;
  end;
  rows:=h-3;
  if full then begin
    {Draw box}
    DrawBox(x,y,w,h,bmSingle,attrBox,true);
    {Draw title}
    title:=dir;
    if length(title)>w-3 then begin
      title:='...'+copy(title,length(title)-(w-8),w-7);
    end;
    title:='['+title+']';
    DrawString(x+1,y,title,attrIot);
    charinfo:=GetCharInfo(#179,attrIot);
    for i:=y+1 to (h+y)-2 do
      for col:=0 to Columns-2 do
        DrawChar((x+14)+(col*15),i,charinfo);
    DrawString(x+w-12,y+h-1,'[by XWolf"]',attrIot);
  end;
  {Draw items}
  inc(x);
  inc(y);
  if (Selection div Columns)<Scroll then
    Scroll:=Selection div Columns;
  if (Selection div Columns)>(Scroll+rows) then
    Scroll:=(Selection div Columns)-rows;

  soffset:=Scroll*Columns;
  for i:=soffset to Max do begin
    col:=i mod Columns;
    row:=(i div Columns)-Scroll;
    if row>Rows then break;
    if i=Selection then begin
      attr:=attrSel;
      {$IFDEF INFOBOX}
      InfoShow(i);
      {$ENDIF}
    end else case Files[i].kind of
    ftDirectory:  attr:=attrItm;
    ftExecutable: attr:=attrIex;
    ftBatch:      attr:=attrIba;
    ftOther:      attr:=attrIot;
    end;
    DrawString(x+(15*col),y+row,Files[i].name,attr);
  end;
  if row<=Rows then
    for i:=col+1 to Columns-1 do begin
      DrawString(x+(15*i),y+row,'            ',attrItm);
    end;
  sinput[0]:=#12;
  FillB(sinput[1],12,byte('_'));
  CopyB(Search[1],sinput[1],length(Search));
  DrawString(x,(y+h)-2,' '+sinput+' ',attrSrc);
end;

{Handles the search box}
procedure DoSearch(ch:char);
var
 i,ln:integer;
begin
  if ch=#8 then begin
    Search[0]:=char(byte(Search[0])-1);
  end else
    Search:=Search+UpCase(ch);
  ln:=length(Search);
  if ln>0 then begin
    for i:=0 to Max do
    if copy(Files[i].name,1,ln)=Search then begin
      Selection:=i;
      break;
    end;
  end;
  DrawUI(false);
end;

{Process one command step (return false to exit the application)}
function Command:boolean;
var k:char;
begin
  k:=GetKey;
  if k=#0 then begin
    k:=GetKey;
    {$IFDEF DEBUG_KEY} DrawString(0,0,cstr(byte(k))+'  ',$47); {$ENDIF}
    case k of
    #80:begin {down}
      inc(Selection,Columns);
      if Selection>Max then
      Selection:=Max;
      Search:='';
    end;
    #72:begin {up}
      dec(Selection,Columns);
      if Selection<0 then
      Selection:=0;
      Search:='';
    end;
    #77:begin {left}
      inc(Selection);
      if Selection>Max then
      Selection:=Max;
      Search:='';
    end;
    #75:begin {right}
      dec(Selection);
      if Selection<0 then
      Selection:=0;
      Search:='';
    end;
    #59: {F1}
      ShowHelpDialog;
    end;
    k:=#0;
  end {$IFDEF DEBUG_KEY} else DrawString(0,0,cstr(byte(k))+'  ',$27) {$ENDIF};

  case k of
    #0:; {Fast trap of special key, only for case speedup}
    #13:Endrejuta(Files[Selection]);
    #8 :if length(search)>0 then DoSearch(k) else Patra;
    'a'..'z','A'..'Z','0'..'9','!','.','_','%': DoSearch(k);
  end;
  Command:=k<>#27;
end;

function SetOptions(opts:string):boolean;
var
  i:integer;
begin
  SetOptions:=true;
  for i:=1 to length(opts) do
    case opts[i] of
      'a':optionhidd:=true;
      'm':optionmono:=true;
      'o':optionmono:=false;
      'd':optiondirs:=true;
      'h':SetOptions:=false;
    else {case}
      SetOptions:=false;
    end;
end;

{Initializes the application}
procedure Init;
var
  i:integer;
  p:string;
  doHelp:boolean;
begin
  optionhidd:=false;
  optionmono:=false;
  optiondirs:=false;
  doHelp:=false;
  SetOptions(GetEnv('LSOPT'));
  for i:=1 to paramcount do begin
    p:=paramstr(i);
    if (length(p)>1) and ((p[1]='/') or (p[1]='-')) then begin
      if not SetOptions(copy(p,2,length(p)-1)) then doHelp:=true;
    end else begin {if}
      {$I-}
      chdir(p);
      {$I+}
    end;
  end;
  
  if doHelp then begin
    Writeln('Folder navigator v',version,' by XWolfOverride (2026)');
    Writeln;
    Writeln('use: nav [path] [-options]');
    Writeln(' path:  Valid path');
    Writeln;
    Writeln(' -a: Show hidden');
    Writeln(' -d: Show only directories');
    Writeln(' -m: Disable colors');
    Writeln(' -o: Show colors (default)');
    Writeln(' -h: This help text');
    Halt;
  end;

  if optionmono then begin
    attrBox:=$07;
    attrItm:=$0F;
    attrIex:=$07;
    attrIba:=$07;
    attrIot:=$07;
    attrSel:=$70;
    attrSrc:=$0F;
    attrHelpBox:=$70;
  end else begin
    attrBox:=$1f;
    attrItm:=$1f;
    attrIex:=$12;
    attrIba:=$13;
    attrIot:=$17;
    attrSel:=$2f;
    attrSrc:=$1e;
    attrHelpBox:=$7F;
  end;

  {$IFDEF DEBUG_HIGH}
  SetScreenMode(smHigh);
  {$ENDIF}
  {$IFDEF DEBUG_LOW}
  SetScreenMode(smLow);
  {$ENDIF}
  ScreenMode:=GetScreenMode;
  if ScreenMode=smUnknown then begin
    SetScreenMode(smNormal);
    ScreenMode:=GetScreenMode;
  end;
  Search:='';
{$IFDEF LOWRAM}
  SetCursorPosition(0,0);
{$ELSE}
  SaveScreen(InitialScreen);
{$ENDIF}
{$IFDEF INFOBOX}
  InfoShown:=false;
{$ENDIF}
end;

{Finalizes the application and restore the initial screen}
procedure Done;
begin
{$IFDEF LOWRAM}
  ClearScreen;
{$ELSE}
  RestoreScreen(InitialScreen);
{$ENDIF}
end;

{Application loop}
procedure Execute;
begin
  ReadDirectory;
  DrawUI(true);
  while Command do
    DrawUI(false);
end;

{Entry point}
begin
  Init;
  Execute;
  Done;
end.
