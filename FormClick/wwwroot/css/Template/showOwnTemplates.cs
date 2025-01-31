﻿using static System.Net.Mime.MediaTypeNames;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Remoting.Messaging;

body {
    font-family: Arial, sans-serif;
background - color: #d6dad9;
}

.navbar {
    padding: 15px;
background - color: #34bc91;
}

#searchInput {
    width: 100 %;
max - width: 500px;
border - radius: 5px;
padding: 10px;
}

.comments - container {
    border - top: 1px solid black;
    background - color: #2cc48c;
}

.no - comment {
display: flex;
    justify - content: center;
    align - items: center;
    text - align: center;
    font - family: 'Poppins', sans - serif;
    font - size: 1.2rem;
    font - weight: 500;
color: #555;
    border: 2px dashed #ccc;
    border - radius: 10px;
padding: 10px;
margin: 10px auto;
    max - width: 80 %;
    box - shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
transition: all 0.3s ease;
}

.no - comment:hover {
    color: #2cc48c
    border - color: #2cc48c;
    box - shadow: 0 6px 10px rgba(0, 0, 0, 0.15);
}


.card img, .list-group-item img {
    width: 100 %;
height: auto;
object-fit: cover;
}

.list - group - item {
    margin - top: 10px;
    margin - bottom: 1rem;
display: flex;
    align - items: center;
    background - color: #2cc48c;
    border: 1px solid #7bd8b8;
    border - radius: 8px;
}

.list - group - item img {
    width: 50px;
height: 50px;
border - radius: 50 %;
margin - right: 15px;
}

.creator - small {
    font - weight: bold;
color: #323233;
    font - family: "Roboto", serif;
    font - weight: 500;
    font - style: normal;
}

.list - group - item.title {
    font - weight: bold;
color: #323233;
    font - family: "Roboto", serif;
    font - weight: 500;
    font - style: normal;
}

.card - container {
width: 90 % !important;
}

.card - body {
display: flex;
    align - items: center;
height: 250px;
    background - color: #f9f9f9;
    border - radius: 8px;
}

.card - body img {
    width: 150px;
height: 150px;
object-fit: cover;
margin - right: 20px;
border - radius: 8px;
}

.card - footer - new- comment {
    margin - top: -5px;
    margin - left: 15px;
    margin - right: 20px;
}

.card - title {
    font - size: 1.25rem;
color: #3c3e4f;
}

.card - text {
    margin - top: 10px;
    font - size: 1rem;
color: #6f6f6f;
    font - family: "Roboto", serif;
    font - weight: 500;
    font - style: normal;
}

.card.card - body {
padding: 1.5rem;
}

.category - tag {
    background - color: #46c496;
    color: white;
    border - radius: 12px;
padding: 5px 10px;
    font - size: 0.9rem;
}

.popular - template {
    font - family: "Roboto", serif;
    font - weight: 500;
    font - style: normal;
    margin - bottom: 40px;
}

.card - footer {
display: flex;
    flex - wrap: wrap;
    justify - content: space - between;
padding: 1rem;
    background - color: #2cc48c;
    border - radius: 0px;
}

.card - footer.col - 2 {
display: flex;
    justify - content: center;
    align - items: center;
}

.page - button {
    background - color: #2cc48c!important;
    color: white;
    margin - right: 10px;
    border - radius: 40 %;
border: none;
}

.owner - icon {
width: 30px;
height: 30px;
    border - radius: 50 %;
    background - color: #34bc91;
    display: flex;
    justify - content: center;
    align - items: center;
color: white;
    font - weight: bold;
    margin - right: 10px;
}

.info - block {
display: flex;
    align - items: center;
}

.info - block.owner - icon,
.info - block.category - tag {
    margin - right: 10px;
}

.description - scroll {
    max - height: 90px;
    overflow - y: auto;
    word - wrap: break-word;
    padding - right: 10px;
}

.comment {
    border-bottom: 1px solid #ddd;
    padding: 15px;
font - family: 'Poppins', sans - serif;
color: #fcf9f5;
}

.comment - header {
display: flex;
    justify - content: space - between;
    align - items: center;
    font - size: 0.9rem;
    margin - bottom: 5px;
}

.comment - profile - picture {
width: 5 % !important;
    border - radius: 50 %;
    margin - right: 10px;
}

.comment - username {
    font - weight: 800;
color: #fcf9f5;
    flex - grow: 1;
}

.comment - date {
    font - style: italic;
color: #fcf9f5;
    text - align: right;
}

.comment - text {
    margin - top: 2 %;
    font - size: 1rem;
    line - height: 1.5;
color: #444;
}

.btn - white {
    background - color: white;
color: #333;
    border - color: #ccc;
}

.page - button.selected {
    background - color: #007bff;
    color: white;
    border - color: #007bff;
}

.active {
    background-color: red!important;
}