﻿;layui.extend({setter:"js/config"}).define(["setter","laytpl","layer"],function(A){var i=layui.jquery,d=layui.laytpl,z=layui.layer,e=layui.element,x=layui.setter,n=layui.device(),a=layui.hint(),b=i(window),g=i("body"),y=i("#"+x.container),w="#aly_app_body",s="#aly_app_flexible",u="#aly_app_tabsheader>li",p="aly_system_side_menu",l="aly_layout_tabs",c="layui-show",t="layui-hide",k="layui-this",r="layui-disabled",o="layui-icon-shrink-right",f="layui-icon-spread-left",j="aly-side-spread-sm",B="aly-tabsbody-item",h="aly-side-shrink",v=function(E,H){if(x.pageTabs){var G,F=i(u),D=E.replace(/(^http(s*):)|(\?[\s\S]*$)/g,"");F.each(function(I,J){i(J).attr("lay-id")===E&&(G=true,m.tabsPage.index=I);return !G});var C=function(){e.tabChange(l,E),m.tabsBodyChange(m.tabsPage.index,{url:E,text:H||"New tab"})};G?C():(setTimeout(function(){i(w).append(['<div class="aly-tabsbody-item layui-show">','<iframe src="'+E+'" frameborder="0" class="aly-iframe"></iframe>',"</div>"].join("")),m.tabsPage.index=F.length,C()},10),e.tabAdd(l,{title:"<span>"+H+"</span>",id:E,attr:D}))}else{m.tabsBody(m.tabsPage.index).find(".aly-iframe").get(0).contentWindow.location.href=E}},q=function(D){q.loading=function(E){E.append(this.elemLoad=i('<i class="aly_admin aly_admin-rotate aly_admin-loop layui-icon layui-icon-loading layadmin-loading"></i>'))};q.removeLoad=function(){this.elemLoad&&this.elemLoad.remove()};q.exit=function(E){layui.data(x.tableName,{key:x.request.tokenName,remove:!0}),E&&E()};q.req=function(H){var F=H.success,K=H.error,J=x.request,G=x.response,I=function(){return x.debug?"<br><cite>URL：</cite>"+H.url:""};if(H.data=H.data||{},H.headers=H.headers||{},J.tokenName){var E="string"==typeof H.data?JSON.parse(H.data):H.data;H.data[J.tokenName]=J.tokenName in E?H.data[J.tokenName]:layui.data(x.tableName)[J.tokenName]||"",H.headers[J.tokenName]=J.tokenName in H.headers?H.headers[J.tokenName]:layui.data(x.tableName)[J.tokenName]||""}return delete H.success,delete H.error,i.ajax(i.extend({type:"get",dataType:"json",success:function(L){var N=G.statusCode;if(L[G.statusName]==N.ok){"function"==typeof H.done&&H.done(L)}else{if(L[G.statusName]==N.error){q.exit()}else{var M=["<cite>Error：</cite> "+(L[G.msgName]||"返回状态码异常"),I()].join("");q.error(M)}}"function"==typeof F&&F(L)},error:function(N,M){var L=["请求异常，请重试<br><cite>错误信息：</cite>"+M,I()].join("");q.error(L),"function"==typeof K&&K(res)}},H))};q.popup=function(G){var E=G.success,F=G.skin;return delete G.success,delete G.skin,z.open(i.extend({type:1,title:"提示",content:"",id:"aly-system-view-popup",skin:"layui-layer-admin"+(F?" "+F:""),shadeClose:!0,closeBtn:!1,success:function(I,H){var J=i('<i class="layui-icon" close>&#x1006;</i>');I.append(J),J.on("click",function(){z.close(H)}),"function"==typeof E&&E.apply(this,arguments)}},G))};q.error=function(F,E){return q.popup(i.extend({content:F,maxWidth:300,offset:"t",anim:6,id:"lay_admin_error"},E))};var C=function(E){this.id=E,this.container=i(E?"#"+E:w)};C.prototype.render=function(F,E){var G=this;layui.router();return F=x.views+F+x.engine,i(w).children(".layadmin-loading").remove(),q.loading(G.container),i.ajax({url:F,type:"get",dataType:"html",data:{v:layui.cache.version},success:function(J){J="<div>"+J+"</div>";var I=i(J).find("title"),K=I.text()||(J.match(/\<title\>([\s\S]*)\<\/title>/)||[])[1],H={title:K,body:J};I.remove(),G.params=E||{},G.then&&(G.then(H),delete G.then),G.parse(J),q.removeLoad(),G.done&&(G.done(H),delete G.done)},error:function(H){return q.removeLoad(),G.render.isError?q.error("请求视图文件异常，状态："+H.status):(404===H.status?G.render("template/tips/404"):G.render("template/tips/error"),void (G.render.isError=!0))}}),G};C.prototype.parse=function(I,G,E){var N=this,J="object"==typeof I,H=J?I:i(I),M=J?I:H.find("*[template]"),K=function(P){var R=d(P.dataElem.html()),Q=i.extend({params:F.params},P.res);P.dataElem.after(R.render(Q)),"function"==typeof E&&E();try{P.done&&new Function("d",P.done)(Q)}catch(O){console.error(P.dataElem[0],"\n存在错误回调脚本\n\n",O)}},F=layui.router();H.find("title").remove(),N.container[G?"after":"html"](H.children()),F.params=N.params||{};for(var L=M.length;L>0;L--){!function(){var R=M.eq(L-1),O=R.attr("lay-done")||R.attr("lay-then"),T=d(R.attr("lay-url")||"").render(F),Q=d(R.attr("lay-data")||"").render(F),P=d(R.attr("lay-headers")||"").render(F);try{Q=new Function("return "+Q+";")()}catch(S){a.error("lay-data: "+S.message),Q={}}try{P=new Function("return "+P+";")()}catch(S){a.error("lay-headers: "+S.message),P=P||{}}T?q.req({type:R.attr("lay-type")||"get",url:T,data:Q,dataType:"json",headers:P,success:function(U){K({dataElem:R,res:U,done:O})}}):K({dataElem:R,done:O})}()}return N};C.prototype.autoRender=function(F,E){var G=this;i(F||"body").find("*[template]").each(function(J,H){var I=i(this);G.container=I,G.parse(I,"refresh")})};C.prototype.send=function(F,E){var G=d(F||this.container.html()).render(E||{});return this.container.html(G),this};C.prototype.refresh=function(G){var F=this,E=F.container.next(),H=E.attr("lay-templateid");return F.id!=H?F:(F.parse(F.container,"refresh",function(){F.container.siblings('[lay-templateid="'+F.id+'"]:last').remove(),"function"==typeof G&&G()}),F)};C.prototype.then=function(E){return this.then=E,this};C.prototype.done=function(E){return this.done=E,this};return new C(D)},m={v:"1.2.0",req:q.req,exit:q.exit,tabsPage:{},resizeFn:{},escape:function(C){return String(C||"").replace(/&(?!#?[a-zA-Z0-9]+;)/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/'/g,"&#39;").replace(/"/g,"&quot;")},on:function(D,C){return layui.onevent.call(this,x.modName,D,C)},sendAuthCode:function(F){F=i.extend({seconds:60,elemPhone:"#lay_phone",elemVercode:"#lay_vercode"},F);var E,D=F.seconds,G=i(F.elem),C=function(H){D--,D<0?(G.removeClass(r).html("获取验证码"),D=F.seconds,clearInterval(E)):G.addClass(r).html(D+"秒后重获"),H||(E=setInterval(function(){C(!0)},1000))};F.elemPhone=i(F.elemPhone),F.elemVercode=i(F.elemVercode),G.on("click",function(){var H=F.elemPhone,J=H.val();if(D===F.seconds&&!i(this).hasClass(r)){if(!/^1\d{10}$/.test(J)){return H.focus(),z.msg("请输入正确的手机号")}if("object"==typeof F.ajax){var I=F.ajax.success;delete F.ajax.success}q.req(i.extend(!0,{url:"/auth/code",type:"get",data:{phone:J},success:function(K){z.msg("验证码已发送至你的手机，请注意查收",{icon:1,shade:0}),F.elemVercode.focus(),C(),I&&I(K)}},F.ajax))}})},screen:function(){var C=b.width();return C>1200?3:C>992?2:C>768?1:0},sideFlexible:function(F){var E=y,D=i(s),C=m.screen();"spread"===F?(D.removeClass(f).addClass(o),C<2?E.addClass(j):E.removeClass(j),E.removeClass(h)):(D.removeClass(o).addClass(f),C<2?E.removeClass(h):E.addClass(h),E.removeClass(j)),layui.event.call(this,x.modName,"side({*})",{status:F})},popupRight:function(C){return q.popup.index=z.open(i.extend({type:1,id:"aly_popup_r",anim:-1,title:!1,closeBtn:!1,offset:"r",shade:0.1,shadeClose:!0,skin:"aly_admin aly_admin-rl layui-layer-adminRight",area:"300px"},C))},theme:function(H){var E=(x.theme,layui.data(x.tableName)),C="lay_alycqrs_theme",F=document.createElement("style"),G=d([".layui-side-menu,",".aly-pagetabs .layui-tab-title li:after,",".aly-pagetabs .layui-tab-title li.layui-this:after,",".layui-layer-admin .layui-layer-title,",".aly-side-shrink .layui-side-menu .layui-nav>.layui-nav-item>.layui-nav-child","{background-color:{{d.color.main}} !important;}",".layui-nav-tree .layui-this,",".layui-nav-tree .layui-this>a,",".layui-nav-tree .layui-nav-child dd.layui-this,",".layui-nav-tree .layui-nav-child dd.layui-this a","{background-color:{{d.color.selected}} !important;}",".layui-layout-admin .layui-logo{background-color:{{d.color.logo || d.color.main}} !important;}","{{# if(d.color.header){ }}",".layui-layout-admin .layui-header{background-color:{{ d.color.header }};}",".layui-layout-admin .layui-header a,",".layui-layout-admin .layui-header a cite{color: #f8f8f8;}",".layui-layout-admin .layui-header a:hover{color: #fff;}",".layui-layout-admin .layui-header .layui-nav .layui-nav-more{border-top-color: #fbfbfb;}",".layui-layout-admin .layui-header .layui-nav .layui-nav-mored{border-color: transparent; border-bottom-color: #fbfbfb;}",".layui-layout-admin .layui-header .layui-nav .layui-this:after, .layui-layout-admin .layui-header .layui-nav-bar{background-color: #fff; background-color: rgba(255,255,255,.5);}",".aly-pagetabs .layui-tab-title li:after{display: none;}","{{# } }}"].join("")).render(H=i.extend({},E.theme,H)),D=document.getElementById(C);"styleSheet" in F?(F.setAttribute("type","text/css"),F.styleSheet.cssText=G):F.innerHTML=G,F.id=C,D&&g[0].removeChild(D),g[0].appendChild(F),g.attr("aly-themealias",H.color.alias),E.theme=E.theme||{},layui.each(H,function(J,I){E.theme[J]=I}),layui.data(x.tableName,{key:"theme",value:E.theme})},initTheme:function(D){var C=x.theme;D=D||0,C.color[D]&&(C.color[D].index=D,m.theme({color:C.color[D]}))},tabsBody:function(C){return i(w).find("."+B).eq(C||0)},tabsBodyChange:function(D,C){C=C||{},m.tabsBody(D).addClass(c).siblings().removeClass(c),m.events.rollPage("auto",D),layui.event.call(this,x.modName,"tabsPage({*})",{url:C.url,text:C.text})},resize:function(E){var C=layui.router(),D=C.path.join("-");m.resizeFn[D]&&(b.off("resize",m.resizeFn[D]),delete m.resizeFn[D]),"off"!==E&&(E(),m.resizeFn[D]=E,b.on("resize",m.resizeFn[D]))},runResize:function(){var D=layui.router(),C=D.path.join("-");m.resizeFn[C]&&m.resizeFn[C]()},delResize:function(){this.resize("off")},closeThisTabs:function(){m.tabsPage.index&&i(u).eq(m.tabsPage.index).find(".layui-tab-close").trigger("click")},fullScreen:function(){var D=document.documentElement,C=D.requestFullScreen||D.webkitRequestFullScreen||D.mozRequestFullScreen||D.msRequestFullscreen;"undefined"!=typeof C&&C&&C.call(D)},exitScreen:function(){document.documentElement;document.exitFullscreen?document.exitFullscreen():document.mozCancelFullScreen?document.mozCancelFullScreen():document.webkitCancelFullScreen?document.webkitCancelFullScreen():document.msExitFullscreen&&document.msExitFullscreen()},events:{flexible:function(E){var C=E.find(s),D=C.hasClass(f);m.sideFlexible(D?"spread":null)},refresh:function(){var E=".aly-iframe",D=i("."+B).length;m.tabsPage.index>=D&&(m.tabsPage.index=D-1);var C=m.tabsBody(m.tabsPage.index).find(E);C.get(0).contentWindow.location.reload()},serach:function(C){C.off("keypress").on("keypress",function(D){if(this.value.replace(/\s/g,"")&&13===D.keyCode){var F=C.attr("lay-action"),E=C.attr("lay-text")||"搜索";F+=this.value,E=E+' <span style="color: #FF5722;">'+m.escape(this.value)+"</span>",v(F,E),m.events.serach.keys||(m.events.serach.keys={}),m.events.serach.keys[m.tabsPage.index]=this.value,this.value===m.events.serach.keys[m.tabsPage.index]&&m.events.refresh(C),this.value=""}})},message:function(C){C.find(".layui-badge-dot").remove()},theme:function(){m.popupRight({id:"aly_popup_theme",success:function(){q(this.id).render("system/theme")}})},note:function(E){var C=m.screen()<2,D=layui.data(x.tableName).note;m.events.note.index=q.popup({title:"便签",shade:0,offset:["41px",C?null:E.offset().left-250+"px"],anim:-1,id:"aly_note",skin:"aly-note aly_admin aly_admin-upbit",content:'<textarea placeholder="内容"></textarea>',resize:!1,success:function(I,G){var H=I.find("textarea"),F=void 0===D?"便签中的内容会存储在本地，这样即便你关掉了浏览器，在下次打开时，依然会读取到上一次的记录。是个非常小巧实用的本地备忘录":D;H.val(F).focus().on("keyup",function(){layui.data(x.tableName,{key:"note",value:this.value})})}})},fullscreen:function(F){var C="layui-icon-screen-full",E="layui-icon-screen-restore",D=F.children("i");D.hasClass(C)?(m.fullScreen(),D.addClass(E).removeClass(C)):(m.exitScreen(),D.addClass(C).removeClass(E))},about:function(){m.popupRight({id:"aly_Popup_about",success:function(){q(this.id).render("system/about")}})},more:function(){m.popupRight({id:"aly_popup_more",success:function(){q(this.id).render("system/more")}})},back:function(){history.back()},setTheme:function(D){var C=D.data("index");D.siblings(".layui-this").data("index");D.hasClass(k)||(D.addClass(k).siblings(".layui-this").removeClass(k),m.initTheme(C))},rollPage:function(H,E){var D=i("#aly_app_tabsheader"),I=D.children("li"),C=(D.prop("scrollWidth"),D.outerWidth()),F=parseFloat(D.css("left"));if("left"===H){if(!F&&F<=0){return}var G=-F-C;I.each(function(L,K){var M=i(K),J=M.position().left;if(J>=G){return D.css("left",-J),!1}})}else{"auto"===H?!function(){var K,J=I.eq(E);if(J[0]){if(K=J.position().left,K<-F){return D.css("left",-K)}if(K+J.outerWidth()>=C-F){var L=K+J.outerWidth()-(C-F);I.each(function(O,N){var P=i(N),M=P.position().left;if(M+F>0&&M-F>L){return D.css("left",-M),!1}})}}}():I.each(function(L,J){var M=i(J),K=M.position().left;if(K+M.outerWidth()>=C-F){return D.css("left",-K),!1}})}},leftPage:function(){m.events.rollPage("left")},rightPage:function(){m.events.rollPage()},closeThisTabs:function(){var C=parent===self?m:parent.layui.admin;C.closeThisTabs()},closeOtherTabs:function(D){var C="lay-system-pagetabs-remove";"all"===D?(i(u+":gt(0)").remove(),i(w).find("."+B+":gt(0)").remove(),i(u).eq(0).trigger("click")):(i(u).each(function(F,E){F&&F!=m.tabsPage.index&&(i(E).addClass(C),m.tabsBody(F).addClass(C))}),i("."+C).remove())},closeAllTabs:function(){m.events.closeOtherTabs("all")},shade:function(){m.sideFlexible()},signout:function(){q.req({url:x.request.apiUrl+"/auth/signout",type:"post",done:function(C){q.exit(function(){location.href="auth"})}})}}};!function(){var C=layui.data(x.tableName);C.theme?m.theme(C.theme):x.theme&&m.initTheme(x.theme.initColorIndex),"pageTabs" in layui.setter||(layui.setter.pageTabs=!0),x.pageTabs||(i("#aly_app_tabs").addClass(t),y.addClass("aly-tabspage-none")),n.ie&&n.ie<10&&q.error("IE"+n.ie+"下访问可能不佳，推荐使用：Chrome / Firefox / Edge 等高级浏览器",{offset:"auto",id:"lay_error_id"})}(),b.on("resize",layui.data.resizeSystem),m.on("tabsPage(setMenustatus)",function(G){var E=G.url,D=function(I){return{list:I.children(".layui-nav-child"),a:I.children("*[lay-href]")}},H=i("#"+p),C="layui-nav-itemed",F=function(I){I.each(function(M,P){var K=i(P),L=D(K),O=L.list.children("dd"),J=E===L.a.attr("lay-href");if(O.each(function(T,W){var R=i(W),S=D(R),V=S.list.children("dd"),Q=E===S.a.attr("lay-href");if(V.each(function(aa,ac){var Y=i(ac),Z=D(Y),ab=E===Z.a.attr("lay-href");if(ab){var X=Z.list[0]?C:k;return Y.addClass(X).siblings().removeClass(X),!1}}),Q){var U=S.list[0]?C:k;return R.addClass(U).siblings().removeClass(U),!1}}),J){var N=L.list[0]?C:k;return K.addClass(N).siblings().removeClass(N),!1}})};H.find("."+k).removeClass(k),m.screen()<2&&m.sideFlexible(),F(H.children("li"))}),e.on("tab("+l+")",function(C){m.tabsPage.index=C.index}),e.on("nav("+p+")",function(C){C.siblings(".layui-nav-child")[0]&&y.hasClass(h)&&(m.sideFlexible("spread"),z.close(C.data("index"))),m.tabsPage.type="nav"}),e.on("nav(aly_pagetabs_nav)",function(D){var C=D.parent();C.removeClass(k),C.parent().removeClass(c)}),e.on("tabDelete("+l+")",function(D){var C=i(u+".layui-this");D.index&&m.tabsBody(D.index).remove(),m.tabsBodyChange(C.index,{url:C.attr("lay-attr")});m.delResize()}),g.on("click",u,function(){var D=i(this),C=D.index();m.tabsPage.type="tab",m.tabsPage.index=C,m.tabsBodyChange(C,{url:D.attr("lay-attr")})}),g.on("click","*[lay-href]",function(){var E=i(this),D=E.attr("lay-href"),C=E.attr("lay-text");layui.router();m.tabsPage.elem=E;v(D,C||E.text()),D===m.tabsBody(m.tabsPage.index).find("iframe").attr("src")&&m.events.refresh()}),g.on("click","*[lay-event]",function(){var D=i(this),C=D.attr("lay-event");m.events[C]&&m.events[C].call(this,D)}),g.on("mouseenter","*[lay-tips]",function(){var F=i(this);if(!F.parent().hasClass("layui-nav-item")||y.hasClass(h)){var E=F.attr("lay-tips"),D=F.attr("lay-offset"),G=F.attr("lay-direction"),C=z.tips(E,this,{tips:G||1,time:-1,success:function(I,H){D&&I.css("margin-left",D+"px")}});F.data("index",C)}}).on("mouseleave","*[lay-tips]",function(){z.close(i(this).data("index"))});layui.data.resizeSystem=function(){z.closeAll("tips"),layui.data.resizeSystem.lock||setTimeout(function(){q().autoRender(),m.sideFlexible(m.screen()<2?"":"spread"),delete layui.data.resizeSystem.lock},100),layui.data.resizeSystem.lock=true};A("admin",m)});