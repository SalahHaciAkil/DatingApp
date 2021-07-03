import { HttpClient } from '@angular/common/http';
import { Component, Input, OnInit } from '@angular/core';
import { User } from 'src/app/interfaces/User';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit {
  @Input() registerMode: boolean;
  constructor() {
  }

  ngOnInit(): void {
  }

  registerModeToggle() {
    this.registerMode = true;
  }
  cancelModeToggle(event: boolean) {
    alert(this.registerMode);
    this.registerMode = event;

  }
}
