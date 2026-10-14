insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode) select 10,'800米','分.秒',2,1,2.00,9.00,1 where not exists (select * from PhysicalItem where id=10)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode) select 11,'1000米','分.秒',2,1,2.00,9.00,1 where not exists (select * from PhysicalItem where id=11)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode) select 18,'一分钟跳绳','次/分',0,1,0,300,2 where not exists (select * from PhysicalItem where id=18)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode) select 15,'掷实心球','米',2,1,1.0,25.0,2 where not exists (select * from PhysicalItem where id=15)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode) select 6,'立定跳远','厘米',0,1,50,400,2 where not exists (select * from PhysicalItem where id=6)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode) select 5,'50米','秒',1,1,0,20.0,1  where not exists (select * from PhysicalItem where id=5)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode) select 12,'一分钟仰卧起坐','次/分',0,1,0,99,2  where not exists (select * from PhysicalItem where id=12)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode) select 14,'引体向上','次',0,1,0,99,2  where not exists (select * from PhysicalItem where id=14)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode) select 13,'400米','分.秒',2,1,0.45,4.00,1  where not exists (select * from PhysicalItem where id=13)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode) select 7,'坐位体前屈','厘米',1,1, -30,40,2 where not exists (select * from PhysicalItem where id=7)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode) select 8,'握力','公斤',1,1,1,100,2 where not exists (select * from PhysicalItem where id=8)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode) select 9,'50米×8往返跑','分.秒',2,1,0.45,4.00,1  where not exists (select * from PhysicalItem where id=9)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode) select 1,'身高','厘米',1,1,80,250,null  where not exists (select * from PhysicalItem where id=1)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode) select 2,'体重','公斤',1,1,14,200,null  where not exists (select * from PhysicalItem where id=2)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode) select 4,'肺活量','毫升',0,1,0,9999,2  where not exists (select * from PhysicalItem where id=4)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode) select 3,'台阶测试','次',1,1,20,99,2  where not exists (select * from PhysicalItem where id=3)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode) select 16,'丢沙包','米',1,1,0,50,2  where not exists (select * from PhysicalItem where id=16)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode) select 17,'25米*2往返跑','秒',1,1,5.0,20.0,1  where not exists (select * from PhysicalItem where id=17)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode) select 19,'篮球运球','秒',2,1,1,60,1 where not exists (select * from PhysicalItem where id=19)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode) select 20,'足球运球','秒',2,1,1,60,1 where not exists (select * from PhysicalItem where id=20)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode) select 21,'排球垫球','次',0,1,0,99,2  where not exists (select * from PhysicalItem where id=21)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode) select 22,'30秒踢键子','次',0,1,0,99,2 where not exists (select * from PhysicalItem where id=22)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode) select 23,'足球颠球','次',0,1,0,99,2 where not exists (select * from PhysicalItem where id=23)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode) select 70,'一分钟俯卧撑','次/分',0,1,0,900,2 where not exists (select * from PhysicalItem where id=70)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode,Formula) select 24,'BMI指数','',1,1,null,null,null,'([体重] * 10000)/([身高] * [身高])' where not exists (select * from PhysicalItem where id=24)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode,Formula) select 25,'肺活量体重指数','',1,1,null,null,2,'[肺活量]/[体重]' where not exists (select * from PhysicalItem where id=25)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode,Formula) select 26,'握力体重指数','',1,1,null,null,2,'([握力] * 100) / [体重]' where not exists (select * from PhysicalItem where id=26)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode,Formula) select 71,'左眼裸眼视力','',1,1,0,10,2,null where not exists (select * from PhysicalItem where id=71)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode,Formula) select 72,'右眼裸眼视力','',1,1,0,10,2,null where not exists (select * from PhysicalItem where id=72)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode,Formula) select 73,'100米跑','秒',2,1,5,50,1,null where not exists (select * from PhysicalItem where id=73)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode,Formula) select 74,'200米跑','秒',2,1,5,100,1,null where not exists (select * from PhysicalItem where id=74)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode,Formula) select 80,'反应时','秒',2,1,null,null,1,null where not exists (select * from PhysicalItem where id=80)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode,Formula) select 81,'纵跳摸高','厘米',2,1,null,null,2,null where not exists (select * from PhysicalItem where id=81)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode,Formula) select 82,'闭眼单脚站立','秒',2,1,null,null,2,null where not exists (select * from PhysicalItem where id=82)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode,Formula) select 75,'60米跑','秒',2,1,0,30,1,null where not exists (select * from PhysicalItem where id=75)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode,Formula) select 76,'100米跨栏','秒',2,1,0,30,1,null where not exists (select * from PhysicalItem where id=76)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode,Formula) select 77,'110米跨栏','秒',2,1,0,30,1,null where not exists (select * from PhysicalItem where id=77)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode,Formula) select 79,'4*100米跑','秒',2,1,0,120,1,null where not exists (select * from PhysicalItem where id=79)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode,Formula) select 83,'背力计','公斤',2,1,0,500,2,null where not exists (select * from PhysicalItem where id=83)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode,Formula) select 84,'五米三向','秒',2,1,0,60,1,null where not exists (select * from PhysicalItem where id=84)
go

--2017-8-12 增加体成分分析项目
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode,Formula) select 151,'体脂肪率(TBF)','%',1,1,0,100,0,null where not exists (select * from PhysicalItem where id=151)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode,Formula) select 152,'内脂指数(VFI)','',1,1,0,100,0,null where not exists (select * from PhysicalItem where id=152)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode,Formula) select 153,'体水分率(TBW)','%',1,1,0,100,0,null where not exists (select * from PhysicalItem where id=153)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode,Formula) select 154,'骨骼肌率(SM)','%',1,1,0,100,0,null where not exists (select * from PhysicalItem where id=154)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode,Formula) select 155,'骨矿含量(BMC)','KG',1,1,0,100,0,null where not exists (select * from PhysicalItem where id=155)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode,Formula) select 156,'基础代谢(BMR)','',0,1,0,10000,0,null where not exists (select * from PhysicalItem where id=156)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode,Formula) select 157,'上肢平衡','',3,1,0,100,0,null where not exists (select * from PhysicalItem where id=157)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode,Formula) select 158,'下肢平衡','',3,1,0,100,0,null where not exists (select * from PhysicalItem where id=158)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode,Formula) select 159,'全身平衡','',3,1,0,100,0,null where not exists (select * from PhysicalItem where id=159)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode,Formula) select 160,'体成份综合评分','',1,1,0,100,2,null where not exists (select * from PhysicalItem where id=160)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode,Formula) select 161,'生理年龄','',0,1,0,100,1,null where not exists (select * from PhysicalItem where id=161)
go

--20171010 运动处方增加一列得分等级
alter table SportSuggestion add Rank nvarchar(50)
go

--增加25米往返跑，抛绣球，踢键子三个测试科目 20180501
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode,Formula) select 85,'25米往返跑','秒',2,1,0,100,1,null where not exists (select * from PhysicalItem where id=85)
go

insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode,Formula) select 86,'抛绣球','次',0,1,0,100,2,null where not exists (select * from PhysicalItem where id=86)
go

insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode,Formula) select 87,'踢键子','个',0,1,0,300,2,null where not exists (select * from PhysicalItem where id=87)
go

--20180829 学生成绩增加一列NOTE
alter table PhysicalScore add Note nvarchar(512)
go

--20181019 运动建议表增加性别和评价两列
alter table SportSuggestion add Sex nvarchar(50)
go

alter table SportSuggestion add Evaluation nvarchar(512)
go

--20181127 体测项目增加开始结束日期字段
alter table PhysicalProject add StartDate datetime
go

alter table PhysicalProject add EndDate datetime
go

--20181227 项目增加测试次数列
alter table PhysicalItem add TestCount int
go

--20190219 操作员增加考点列
alter table [Operator] add PhysicalProject nvarchar(50)
go

--20190225 增加斜体引体 向上
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode,Formula) select 150,'斜身引体','',0,1,0,100,2,null where not exists (select * from PhysicalItem where id=150)
go

--20190226 项目增加简称列
alter table PhysicalItem add ShortName nvarchar(50)
go

alter table StudentGroup add PlanDate nvarchar(50)
go

alter table StudentGroup add CheckTime Datetime
go

--2019-2-27 增加学生检录记录表
CREATE TABLE [StudentCheckRecord] (
  [ID] uniqueidentifier NOT NULL, 
  [CheckTime] datetime NOT NULL, 
  [ProjectID] varchar(50) NOT NULL, 
  [CheckResult] nvarchar(50) NOT NULL, 
  [StudentID] nvarchar(50) NOT NULL, 
  [Name] nvarchar(50) NOT NULL, 
  [Facility] nvarchar(50) NOT NULL, 
  [Sex] nchar(1) NOT NULL, 
  [GroupID] nvarchar(50), 
  [Channel] int, 
  [Operator] nvarchar(50), 
  [Note] nvarchar(512), 
  CONSTRAINT [sqlite_autoindex_StudentCheckRecord_1] PRIMARY KEY ([ID]));
go

--2019-3-22 增加学生分组表
CREATE TABLE [StudentGroupPairV2] (
  [ID] GUID NOT NULL, 
  [GroupID] GUID NOT NULL, 
  [StudentID] CHAR NOT NULL, 
  [Channel] INT, 
  [State] INT NOT NULL, 
  [CheckTime] DATE, 
  [Memo] CHAR, 
  CONSTRAINT [] PRIMARY KEY ([ID]))
go

CREATE TABLE [StudentGroupV2] (
  [ID] GUID NOT NULL, 
  [ProjectID] CHAR NOT NULL, 
  [Number] INTEGER NOT NULL, 
  [Sex] TINYINT, 
  [PhysicalItems] CHAR, 
  [PlanDate] CHAR, 
  [FacilityID] CHAR, 
  CONSTRAINT [] PRIMARY KEY ([ID]))
go

---2019-8-12 增加视力四个项目
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode,Formula) select 90,'左眼串镜','',0,1,0,9,0,null where not exists (select * from PhysicalItem where id=90)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode,Formula) select 91,'右眼串镜','',0,1,0,9,0,null where not exists (select * from PhysicalItem where id=91)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode,Formula) select 92,'左眼屈光不正','',0,1,0,9,0,null where not exists (select * from PhysicalItem where id=92)
go
insert into PhysicalItem (ID,Name,Unit,PointCount,Hasmachine,[Min],[Max],SortMode,Formula) select 93,'右眼屈光不正','',0,1,0,9,0,null where not exists (select * from PhysicalItem where id=93)
go
update physicalItem set [Name]='左眼裸眼视力' where id=71 and [Name]<>'左眼裸眼视力'
go
update physicalItem set [Name]='右眼裸眼视力' where id=72 and [Name]<>'右眼裸眼视力'
go

update physicalItem set [min]=-1 where id=90  and [Min]=0
go

update physicalItem Set [Min]=-1 where id=91 and [Min]=0
go

update physicalItem set [Min]=0 where id=71 and [Min]>0
go

update physicalItem set [Min]=0 where id=72 and [Min]>0
go


--2020-1-11 增加成绩仲裁功能表
CREATE TABLE [Discussion] (
  [ID] CHAR NOT NULL, 
  [CreateTime] DATE NOT NULL, 
  [ProjectID] CHAR NOT NULL, 
  [StudentID] CHAR NOT NULL, 
  [Name] CHAR NOT NULL, 
  [FacilityID] CHAR NOT NULL, 
  [FacilityName] CHAR NOT NULL, 
  [ScoreID] GUID NOT NULL, 
  [Score] CHAR, 
  [ScoreTime] DATE NOT NULL, 
  [PhysicalItem] INT NOT NULL, 
  [Description] CHAR, 
  [ResolveType] INT NOT NULL, 
  [NewScore] CHAR, 
  [Operator] CHAR, 
  [Approver] CHAR, 
  CONSTRAINT [] PRIMARY KEY ([ID]))

go































