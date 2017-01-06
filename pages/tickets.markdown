---
layout: page
title: Got Feedback or Bugs?
permalink: /tickets/
categories: []
comments: []
tags: []
---

<style>
    ul.contact {
      margin:0;
      padding:0;
        list-style-type:none;
    
    }
    ul.contact li {
      position:relative;
      overflow:hidden;
      float:left;
        margin:0 20px 20px 0;
      padding:10px;
      width:45%;
      height:148px;
      background-color: #ddeedd;
    }
    ul.contact h2 {
      position:relative;
      z-index:2;
        margin:0;
      font-family:"Bangers";
      font-size:24px;
      font-weight:normal;
      
    }
    ul.contact li p {
      position:relative;
      z-index:1;
      /*display:none;*/
    }
    /*ul.contact li:active p,
    ul.contact li:hover p {
    	display:block;
    }*/
    
    ul.contact li:after {
      position:absolute;
      z-index:0;
      color:#fff;
      top:-30px;
      right:-20px;
      font-family:"FontAwesome";
      
      font-size:220px;
      transform:rotate(10deg);
    }
    ul.contact li.question:after {
    	content:"\f128";
    }
    ul.contact li.feature:after {
      content:"\f0eb";
    
    }
    ul.contact li.bug:after {
      content:"\f188";
    }
    ul.contact li.follow:after {
    
      content:"\f099";
    }
    ul.contact .letsgo {
      position:absolute;
      z-index:2;
    	right:20px;
      bottom:20px;
      font-family:"Bangers";
      font-size:24px;
    }
    
    
    </style>
  
	<p>
    We really love to hear constructive feedback. But please don't expect an instant response. Since Greenshot is used by millions of people, it's not always possible to respond timely. We hope for your understanding. Before getting in touch, please have a look at our <a href="/faq/">FAQ</a> and <a href="/help/">help</a> pages, or browse bug reports and feature requests submitted by other users. This way, you might find an answer faster and at the same time save us some efforts we can instead put into Greenshot. Thanks a lot. <i class="fa fa-heart"></i>
  </p>
  
  <ul class="contact">
    <li class="bug">
      <h2>Got a bug to report?</h2>
      <p>
        File a bug report in our Jira ticketing system
        or browse bug reports submitted by others.
      </p>
      <a href="https://greenshot.atlassian.net/browse/BUG" class="letsgo">Let's track it down!</a>
    </li>
    <li class="feature">
      <h2>Got a cool feature idea?</h2>
      <p>
        Create a feature request in our Jira ticketing system
        or browse and upvote ideas submitted by others.
      </p>
      <a href="https://greenshot.atlassian.net/browse/FEATURE" class="letsgo">We'd love to hear about it!</a>
    </li>
    <li class="question">
      <h2>Got a question about using Greenshot?</h2>
      <p>
        Ask your question on superuser.com! We and
        a lot of friendly Greenshot users in the community
        will try to help quickly.
      </p>
      <a href="http://superuser.com/questions/ask?tags=greenshot" class="letsgo">Okay, let's go!</a>
    </li>
    <li class="follow">
      <h2>Gotta stay up to date?</h2>
      <p>
        Follow us on Twitter for the latest news and other bits of information.
      </p>
      <a href="https://twitter.com/greenshot_tool" class="letsgo">Beep beep!</a>
    </li>
    <p>
    </p>
  </ul> 
