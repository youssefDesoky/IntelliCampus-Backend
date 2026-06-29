--aggregate function (built-in) functions
--max, min,avg,sum,count

--Count
--count ignoring nulls values
-- in iti

--total number of students
select count(*) [total number of students]
from student

select count(st_id)
from student

--total number of courses
select count(*)
from course

select count(crs_id)
from course


--use mycompany
--select the number of employees by id
select count(ssn)
from employee

--select the number of subervisors
select count(superssn)
from employee


--sum
--by all or by distinct
--all -> using all values
--distinct -> no values will be used more than one time


--use iti
-- sum of salaries in instructor table
select sum(salary) [SumOfSalaries]
from instructor

--sum of courses durations in course table
select sum(crs_duration)
from course


--avg
--use iti
--select avg salaries from instuctor table

select avg(salary)
from instuctor
--avg=sum/count
select sum(salary)/count(salary)
from instructor

--avg age in student table
select avg(st_age)
from student
 
select avg(*) --invalid
from student

select sum(*)  --invalid
from student


--select avg duration from course table
select avg(crs_duration)
from course
--avg=sum/count
select sum(crs_duration)/count(*)
from course

--use iti
--max
--max duration of course
select max(crs_duration)
from course

--max salary from instructors
select max(salray)
from instructor

--max student age
select max(st_age)
from student

--max student fname
select mAX (st_fname)
from student


--min
--min duration of course
select max(crs_duration)
from course

--min salary from instructors
select max(salray)
from instructor

--min student age
select max(st_age)
from student

--min student fname
select min (st_fname)
from student

--partiton by
--use iti
--select min salary for each department
--group by=distincit+partiton by
select distinct dept_id,min(salary) over (partition by dept_id)
from instructor
where salary is not null

--null functions
-- to replace null with specific values

--1) isnull

--select isnull(value i will change,the value will change the first value)
--select isnull(weight,50)

--is student lname null add(not found)
select isnull(st_lname,'not found')
from student

select st_lname
from student

-- if student lname is null replace with fname
select isnull(st_lname,st_fname)
from student

--if student lname is null replace with fname and the same for lnamenot found
select isnull(st_lname,isnull(st_fname,'not found'))
from student

select isnull(st_age,0)
from student

--invalid conversion
select isnull(st_age,st_fname)
from student

-- it can be because it will be made by casting
select isnull(st_age,13.5)
from student

select isnull(salary,100)
from instructor
--coalesce
--if student lname is null replace with fname and the same for lname ,adress, not found
select coalesce(st_fname,st_lname,st_address,'not found')
from student

--concat
-- +==>null

select st_fname + ' '+lname [full name]
from student

select ISNULL(st_fname,'fname not found')+ ' '+ISNULL(st_lname,'lname not found')
from student

--casting
--convert int data type to nvarchar
select st_age +' '+ st_fname
from student

select isnull(convert(nvarchar(max),st_age),'not found')+' '+ISNULL( st_fname,'not found')
from student

--concat string function
select concat(st_age,' ',st_fname)--cast any thing to sting and null will be empty
from student


--concat_ws: take first parameter to sepereate with
select concat_ws(',',st_age,st_fname,st_lname)--cast any thing to sting and null will be empty
from student


--casting
select cast(st_age as nvarchar(max)) + ' ' +st_fname

--difference between cast and convertt
declare @today date ='11-06-2024' -- local variable
select convert (varchar(max),@today)
select cast(@today as varchar (max))


--convert using 110,111,102,101
declare @today2 date ='11-06-2024'
select convert(varchar(max),@today,110)
select convert(varchar(max),@today,111)
select convert(varchar(max),@today,102)
select convert(varchar(max),@today,101)

--parse
--convert from string to date or time and number typee
select parse('monday,13december 2010' as datetime)
select parse('mariam,shindy 2010' as date)
select parse('monday,13december 2010' as date)


select parse('monday,13december 2010 10:45 PM' as datetime using  'en-us')


--try parse

try_convert
select try_parse('mariam,shindy 2010' as date) 
select try_convert(datetime2, '12/31/2022') as result;
select try_cast('12/31/2022' as datetime0 as result;

select try_convert(XML,4) AS RESULT ;
SELECT TRY_CAST (4 AS XML) AS RESULT;

-- DATE TIME FUNCTIONS
--1 GETDATE
SELECT GETDATE()

--2 GETUTCDATE
SELECT GETUTCDATE()
--3 DAY ,MONTH, YEAR'
SELECT day(GETDATE())
SELECT MONTH(GETDATE())
SELECT YEAR(GETDATE())
select day ('05-12-2024')
select day ('05-55-2024') -- INVALID
SELECT MONTH ('11-20-2002')



-- 4 DATEPART  
--SELECT DATEPART(INTERVAL,GETDATE())
SELECT DATEPART(DAY,GETDATE())
SELECT DATEPART(MONTH,GETDATE())
SELECT DATEPART(YEAR,GETDATE())
SELECT DATEPART(HOUR,GETDATE()) -- THE ONE WHO CAN GET THE HOUR
SELECT DATEPART(YYYY,GETDATE())
SELECT DATEPART(YY,GETDATE())
SELECT DATEPART(HH,GETDATE())
SELECT DATEPART(MINUTE,GETDATE())
SELECT DATEPART(MILLISECOND,GETDATE())
SELECT DATEPART(QQ,GETDATE())
SELECT DATEPART(WEEK,GETDATE())
SELECT DATEPART(WEEKDAY,GETDATE())--1 : sunday
SELECT DATEPART(DAYOFYEAR,GETDATE())



--5 DATE name
select datename(year,getdate())
select datepart(month,getdate())
select datename(month,getdate())-- diff
select datename(weekday,getdate())--diff [day name]
select datename(day,getdate())
-- diff cant concat like date part bec it is string
select datename(QUARTER,getdate())
select datename(DAYOFYEAR,getdate())

--6 idate
--1 valid 0 not valid
-- i will give it a date
select isdate('11-11-2002')--valid 1
select isdate('abc')--invalid 0
select isdate('2017')--valid 1

--7 EOMonth
--endofmonth
--last day in the month i will give
select eomonth (getdate())
select eomonth ('02-02-2002')


--8 datediff
-- diff between two dates
--difference : days,months,years
-- datediff(interval,date,the bigger date)
select datediff(day,'11-02-2002','11-11-2002')-- 9
select datediff(day,'11-11-2002','11-02-2002') -- -9
select datediff(month,'10-10-2010','08-10-2010') -- -2
select datediff(month,'8-10-2010','10-10-2010') -- 2
select datediff(year,'10-10-2002','10-10-2010') -- 8
select datediff(month,'10-10-2010','10-10-2002') -- -8
select datediff(month,'10-10-2010 10:00','10-10-2002 12:00')

--9 string
--format
--upper lower
--len
--concat
--cast all parat]mater to strings , null --> empty string
select  concat(st_fname, ' ',st_lname) 
from student

select  concat_ws(',,,',st_fname,st_lname) 
from student

--format
--date mo3yn f format mo3yn ana 3ayzo
select format(getdate(),'dd MM yyyy')
select format(getdate(),'dddd MMMM yyyy')--asm al day w asm al shahr
select format(getdate(),'dddd MMMM yyyy')
--dd num of day 24
--MM month 5
--dddd saturday
--MMMM may
--yyyy 2025
--yy 25
select format (getdate(),'MMMM')
select format (getdate(),'MMMM','ar')
select format (getdate(),'MMMM','fr')
select format (getdate(),'dddd','ar')-- culture optional paramater
select format (getdate(),'HH')--22
select format (getdate(),'hh')
select format (getdate(),'hh mm ss tt')-- pm or am

--123456789 --> ##,###,###
select format(123456789,'###,###,###')
select format(123456789,'###,,,###,,,###')
select format(123456789,'###//###//###')



--upper/lower
select upper(st_fname),lower(st_fname)
from student


--len
--length: number of characters
select len ('mariam')

--substring
-- 1 based
--substring(string,start,len of substring)
select substring('mariam',1,3)
select substring('mariam',3,4)


--ascii
select ascii('b')
select ascii('B')
select ascii('Belal')--will take first character only

--left/right
--similar as substring
select left ('mariam',3) --mar
select right ('mariam',3) --iam

--ltrim/rtrim/trim
select trim ('        mariam        ')as word
select trim ('        mariam        ')as word
select ltrim ('        mariam')as word
select '        mariam'
select rtrim ('mariam        ')as word
select 'mariam        '

--replace
select replace('mari000am','0','a')--0=>a
select replace('mari000am0','0000','aaaa')--pattern


--reverse
select reverse ('abc')


--group by

-- invalid must be in specific columns
select ins_id
from instructor
group by *

select dept_id,min(salary)-- valid
from instructor
where salary is not null
group by dept_id

select min(salary)-- valid
from instructor
where salary is not null
group by dept_id

--partiton by
select distinct dept_id,min(salary) over(partition by dept_id)
from instructor
where salary is not null


--select count of students in each dep
select dept_id,count(*)
from student
where dept_id is not null
group by dept_id

select distinct dept_id,count(*) over(partition by dept_id)
from student
where dept_id is not null


--group by multible columns
select dept_id,std_address,count(*)
from student
where dept_id is not null and std_address is not null
group by dept_id,std_address
--order by address



select dept_id,std_address,count(*)
from student
where dept_id is not null and std_address is not null
group by std_address,dept_id
--order by id


--having

--count of students in each department and have students more than 2
select dept_id,count(*)
from student
where dept_id is not null
group by dept_id
having count(*)>2
-- having condition in the group after group by

--min salary from each ddepartmen num of instructors>4
select dept_id,min(salary)
from instructor
where dept_id 
group by dept_id
having count(*)>4

--sum salary from each ddepartmen num of instructors>2
select dept_id,sum(salary)
from instructor
where salary is not null 
group by dept_id
having count(*)>4

--sum of salaries where  number of instructors>10
select sum(salary) as sumofsalaries
from instructor
having count(*)>30 

-- Select Each department id with the Sum of salaries in it
Select Dept_Id, Sum(Salary)
From Instructor
Where Salary is not null 
Group by Dept_Id;

Select D.Dept_Id, Sum(Salary)
From Instructor I, Department D
Where D.Dept_Id = I.Dept_Id AND I.Salary is not null
Group by D.Dept_Id;

-- Select department name with the Sum of salaries
Select D.Dept_Name, Sum(I.Salary)
From Instructor I, Department D
Where D.Dept_Id = I.Dept_id and I.Salary is not null
Group by D.Dept_Name;

-- Select supervisor name and the num of students that he supervise 

-- Self join
Select Super.St_Fname [Super name], Count(*) as Students
From Student Super, Student Std
Where Super.St_Id = Std.St_super
Group by Super.St_Fname, Super.St_Id;

--lazem w ana b3ml join  3la name azod kman id 