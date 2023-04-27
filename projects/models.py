from django.db import models

class Project(models.Model):
    name = models.CharField(max_length=50)
    description = models.TextField(max_length=500)
    frontend = models.CharField(max_length=150, null=True, blank=True)
    backend = models.CharField(max_length=150, null=True, blank=True)
    devops = models.CharField(max_length=150, null=True, blank=True)
    url = models.URLField(max_length=200)
    image = models.URLField(max_length=200, null=True, blank=True)
    finished = models.BooleanField()
