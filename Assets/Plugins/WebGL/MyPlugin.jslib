{\rtf1\ansi\ansicpg1252\cocoartf2822
\cocoatextscaling0\cocoaplatform0{\fonttbl\f0\fnil\fcharset0 Menlo-Regular;}
{\colortbl;\red255\green255\blue255;\red247\green247\blue247;\red0\green0\blue0;}
{\*\expandedcolortbl;;\cssrgb\c97647\c97647\c97647;\cssrgb\c0\c0\c0;}
\margl1440\margr1440\vieww11520\viewh8400\viewkind0
\deftab720
\pard\pardeftab720\partightenfactor0

\f0\fs28 \cf0 \cb2 \expnd0\expndtw0\kerning0
var MyPlugin = \{\
    IsMobile: function()\
    \{\
        return UnityLoader.SystemInfo.mobile;\
    \}\
\};\
\
mergeInto(LibraryManager.library, MyPlugin);}