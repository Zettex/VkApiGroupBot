# VkApiGroupBot

Simple Group Bot Callback API written using ASP.NET Core (Web API)

### How to use:

* Download project
* Add to appsettings.json your group access token, confirmation code, your Vk Id

### Deploy in Heroku:

* Create Project in Heroku
* Download Heroku CLI
* Go to terminal
* Authorize: $ heroku login
* Go to project catalog: $ cd (path to the project)
* Init git repo in the current catalog : $ git init
* Connect to remote repo: $ heroku git:remote -a (project name)
* Use buildpacks: $ heroku buildpacks:set https://github.com/jincod/dotnetcore-buildpack.git -a (project name)
* $ git add .
* $ git commit -am "your text"
* $ git push heroku master
