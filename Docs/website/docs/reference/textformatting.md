---
layout: article
title: Text Formatting
aside:
  toc: true
sidebar:
  nav: docs
---

Use the following HTML-like tags in the Info and ErrorInfo interative actions to format text.

| Tag               | Description       | Syntax            | Attributes        |
|:------------------|:------------------|:------------------|:------------------|
| BIG               | Big Text          | &lt;BIG&gt;...&lt;/BIG&gt;	|                   |
| B                 | Bold Text	        | &lt;B&gt;...&lt;/B&gt;	    |                   |
| BR                | Line Break        | &lt;BR&gt;	            |                   |
| CENTER            | Center Text       | &lt;CENTER&gt;...&lt;/CENTER&gt; |                |
| CODE              | Code Text         | &lt;CODE&gt;...&lt;/CODE&gt;	|                   |
| FONT              | Font Change       | &lt;FONT&gt;...&lt;/FONT&gt;	| COLOR="Color string"<br>BGCOLOR="Color string"<br>SIZE="Size adjustment"<br>FACE="Font face name" |
| HR                | Horizontal Rule   | &lt;HR&gt;              | SIZE=Line thickness |
| I                 | Italic Text       | &lt;I&gt;...&lt;/I&gt;        |                   |
| SMALL             | Small Text        | &lt;SMALL&gt;...&lt;/SMALL&gt;|                   |	
| STRIKE            | Strike-through Text | &lt;STRIKE&gt;...&lt;/STRIKE&gt; |              |
| SUB               | Subscript Text	| &lt;SUB&gt;...&lt;/SUB&gt;	|                   |
| SUP               | Superscript Text  | &lt;SUP&gt;...&lt;/SUP&gt;    |                   |	
| U                 | Underlined Text   | &lt;U&gt;...&lt;/U&gt;        |                   |	

The FONT COLOR and BGCOLOR attributes both take a color string value, which is a string in one of three forms:
* "color-name" - Example: "red".
* "hex-value" - Example: "#FF0000".
* "rgb-value" - Example: "255,0,0".

The SIZE attribute for FONT takes relative size adjustments -- plus or minus. For example, SIZE="+4" or SIZE="-2".